using MailKit.Net.Smtp;
using MailKit.Security;
using MicroM.Configuration;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.DataDictionary.StatusDefinitions;
using MicroM.Extensions;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using System.Net.Sockets;
using System.Text.Json;

namespace MicroM.Web.Services;

public class EmailService(ILogger<EmailService> logger, IBackgroundTaskQueue btq, IMicroMAppConfiguration app_config, IMicroMEncryption encryptor) : IEmailService
{

    public async Task<List<SubmitToQueueResult>> QueueEmail(string app_id, EmailServiceItem send_item, CancellationToken ct, bool start_processing_queue = false)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(send_item.EmailServiceConfigurationId, nameof(send_item.EmailServiceConfigurationId));

        using DatabaseClient? dbc = app_config.GetDatabaseClient(app_id)
            ?? throw new ArgumentNullException($"Can't get a database connection for app {app_id}");
        try
        {
            await dbc.Connect(ct);

            var app = app_config.GetAppConfiguration(app_id)
                ?? throw new ArgumentNullException($"Can't get application configuration for app {app_id}");

            var config = await GetEmailConfiguration(app, send_item.EmailServiceConfigurationId ?? "", dbc, ct)
                ?? throw new ArgumentNullException($"Can't get email configuration for {send_item.EmailServiceConfigurationId}");

            var sender_email = send_item.SenderEmail ?? config.vc_default_sender_email
                ?? throw new ArgumentNullException($"Can't get sender email for {send_item.EmailServiceConfigurationId}");


            var esq = new EmailServiceQueue(dbc, schema_name: app.SchemaConfiguration.DDSchema);
            esq.Def.c_email_configuration_id.Value = send_item.EmailServiceConfigurationId ?? "";

            esq.Def.vc_sender_email.Value = sender_email;
            esq.Def.vc_sender_name.Value = send_item.SenderName ?? config.vc_default_sender_name ?? "";

            esq.Def.vc_subject.Value = send_item.SubjectTemplate ?? "";
            esq.Def.vc_message.Value = send_item.MessageTemplate ?? "";

            esq.Def.vc_json_destination_and_tags.Value = send_item.Destinations != null ? JsonSerializer.Serialize<EmailServiceDestination[]>(send_item.Destinations) : "";

            var result = await esq.ExecuteProc<SubmitToQueueResult>(esq.Def.emq_SubmitToQueue, ct);

            if (start_processing_queue)
            {
                await StartProcessingQueue(app_id, ct);
            }

            return result;

        }
        finally
        {
            await dbc.Disconnect();
        }
    }

    private async Task UpdateQueueStatus(EmailServiceQueue emq, EmailQueuedItem item, string new_status, string? last_error, CancellationToken ct)
    {
        emq.Def.c_email_queue_id.Value = item.c_email_queue_id;
        emq.Def.c_emailstatus_id.Value = new_status;
        emq.Def.vc_last_error.Value = last_error;
        emq.Def.dt_lu.Value = item.dt_lu;

        try
        {
            await emq.Client.Connect(ct);
            await emq.UpdateData(ct, true);
            await emq.GetData(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmailService.UpdateQueueStatus: Error updating email_queue_id {email_queue_id} to status {new_status} with lu {lu}", item.c_email_queue_id, new_status, item.dt_lu);
        }
        finally
        {
            try
            {
                await emq.Client.Disconnect();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EmailService.UpdateQueueStatus: Error disconnecting from database for email_queue_id {email_queue_id}", item.c_email_queue_id);
            }
        }

        item.dt_lu = emq.Def.dt_lu.Value;
    }

    internal class SendMailResult
    {
        public bool failed;
        public bool should_retry;
        public string? last_error;
        public string? send_result;
    };

    internal static async Task<SendMailResult> SendEmail(EmailQueuedItem item, EmailServiceConfigurationData config, CancellationToken ct)
    {
        var mailMessage = new MimeMessage();
        mailMessage.From.Add(new MailboxAddress(item.vc_sender_name, item.vc_sender_email));
        mailMessage.To.Add(new MailboxAddress(item.vc_destination_email, item.vc_destination_email));
        mailMessage.Subject = item.vc_subject;
        mailMessage.Body = new TextPart(TextFormat.Html) { Text = item.vc_message };

        using var client = new SmtpClient();
        SendMailResult result = new();
        try
        {
            if (config.i_smtp_port == 587)
            {
                await client.ConnectAsync(config.vc_smtp_host!, config.i_smtp_port, SecureSocketOptions.StartTls, ct);

            }
            else
            {
                await client.ConnectAsync(config.vc_smtp_host!, config.i_smtp_port, config.bt_use_ssl, ct);
            }

            if (!string.IsNullOrEmpty(config.vc_user_name) && !string.IsNullOrEmpty(config.vc_password))
            {
                await client.AuthenticateAsync(config.vc_user_name, config.vc_password, ct);
            }

            result.send_result = await client.SendAsync(mailMessage, ct);

        }
        catch (AuthenticationException ex)
        {
            result.failed = true;
            result.should_retry = false;
            result.last_error = ex.ToString();
        }
        catch (SmtpCommandException ex)
        {
            result.failed = true;
            result.should_retry = false;
            result.last_error = ex.ToString();
        }
        catch (SmtpProtocolException ex)
        {
            result.failed = true;
            result.should_retry = true;
            result.last_error = ex.ToString();
        }
        catch (SocketException ex)
        {
            result.failed = true;
            result.should_retry = true;
            result.last_error = ex.ToString();
        }
        catch (Exception ex)
        {
            result.failed = true;
            result.should_retry = false;
            result.last_error = ex.ToString();
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
        return result;
    }

    internal async Task<EmailServiceConfigurationData?> GetEmailConfiguration(ApplicationOption app, string config_id, IEntityClient ec, CancellationToken ct)
    {
        var esc = new EmailServiceConfiguration(ec, encryptor, schema_name: app.SchemaConfiguration.DDSchema);
        esc.Def.c_email_configuration_id.Value = config_id;
        await esc.GetData(ct);

        var new_config = esc.Def.Columns.MapColumnData<EmailServiceConfigurationData>();

        return new_config;
    }

    public Task StartProcessingQueue(string app_id, CancellationToken ct)
    {

        btq.Enqueue($"EmailQueue [{app_id}]", async (ct) =>
        {
            using DatabaseClient? dbc = app_config.GetDatabaseClient(app_id);

            if (dbc == null)
            {
                logger.LogError("EmailService.StartProcessingQueue: Can't get a database connection for {app_id}", app_id);
                return $"Can't get a database connection for {app_id}. No mail will be sent until the service is restarted.";
            }

            var app = app_config.GetAppConfiguration(app_id);
            if (app == null)
            {
                logger.LogError("EmailService.StartProcessingQueue: Can't get application configuration for {app_id}", app_id);
                return $"Can't get application configuration for {app_id}. No mail will be sent until the service is restarted.";
            }

            var emq = new EmailServiceQueue(dbc, schema_name: app.SchemaConfiguration.DDSchema);

            Dictionary<string, EmailServiceConfigurationData> config = [];

            int processed_emails = 0;

            List<EmailQueuedItem>? result = await emq.ExecuteProc<EmailQueuedItem>(emq.Def.emq_qryGetQueuedItems, ct);

            while (result?.Count > 0)
            {
                foreach (var item in result)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return $"Processing Emails for app [{app_id}] cancelled. Total emails processed: {processed_emails}";
                    }

                    if (item.c_email_queue_id != null && item.c_email_configuration_id != null)
                    {
                        try
                        {
                            await UpdateQueueStatus(emq, item, nameof(EmailStatus.PROCESSING), null, ct);

                            if (!config.TryGetValue(item.c_email_configuration_id, out var smtp_config))
                            {
                                smtp_config = await GetEmailConfiguration(app, item.c_email_configuration_id, dbc, ct);
                                if (smtp_config != null && !smtp_config.vc_smtp_host.IsNullOrEmpty() && !smtp_config.vc_user_name.IsNullOrEmpty() && !smtp_config.vc_password.IsNullOrEmpty())
                                {
                                    config[item.c_email_configuration_id] = smtp_config;
                                }
                                else
                                {
                                    if (smtp_config == null)
                                    {
                                        logger.LogWarning("EmailService.StartProcessingQueue [{app_id}]: Can't get email configuration for {email_configuration_id}", app_id, item.c_email_configuration_id);
                                    }
                                    else
                                    {
                                        logger.LogWarning("EmailService.StartProcessingQueue [{app_id}]: Invalid email server configuration for {email_configuration_id}: host = {host}", app_id, item.c_email_configuration_id, smtp_config.vc_smtp_host);
                                    }
                                    await UpdateQueueStatus(emq, item, nameof(EmailStatus.ERROR), "Can't get email configuration", ct);
                                    continue;
                                }
                            }

                            var send_result = await SendEmail(item, smtp_config, ct);

                            if (send_result.failed)
                            {
                                if (send_result.should_retry)
                                {
                                    await UpdateQueueStatus(emq, item, nameof(EmailStatus.RETRY), send_result.last_error, ct);
                                }
                                else
                                {
                                    await UpdateQueueStatus(emq, item, nameof(EmailStatus.ERROR), send_result.last_error, ct);
                                }
                            }
                            else
                            {
                                await UpdateQueueStatus(emq, item, nameof(EmailStatus.SENT), null, ct);
                            }

                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "EmailService.StartProcessingQueue [{app_id}]: Error sending email for email_queue_id {email_queue_id}", app_id, item.c_email_queue_id);
                            await UpdateQueueStatus(emq, item, nameof(EmailStatus.ERROR), ex.ToString(), ct);
                        }

                        processed_emails++;
                    }
                    else
                    {
                        logger.LogWarning("EmailService.StartProcessingQueue [{app_id}]: Missing c_email_queue_id, empty email queue item", app_id);
                    }
                }
                result = await emq.ExecuteProc<EmailQueuedItem>(emq.Def.emq_qryGetQueuedItems, ct);
            }

            //logger.LogInformation("EmailService.StartProcessingQueue: Processing Emails finished. Total emails processed: {processed_emails}", processed_emails);

            return $"Processing Emails for [{app_id}] finished. Total emails processed: {processed_emails}";
        }, true);

        return Task.CompletedTask;
    }
}
