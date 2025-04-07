using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.StatusDefs;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    public record EmailQueuedItem
    {
        public string c_email_configuration_id = "";
        public string c_email_queue_id = "";
        public string c_emailstatus_id = "";
        public string vc_destination_email = "";
        public string vc_destination_name = "";
        public string vc_sender_email = "";
        public string vc_sender_name = "";
        public string vc_subject = "";
        public string vc_message = "";
        public DateTime dt_lu;
    }

    public record SubmitToQueueResult
    {
        public string? c_email_queue_id;
        public string? reference_id;
    }

    public class EmailServiceQueueDef : EntityDefinition
    {
        public EmailServiceQueueDef() : base("emq", nameof(EmailServiceQueue)) { }

        public readonly Column<string> c_email_queue_id = Column<string>.PK(autonum: true);
        public readonly Column<string> c_email_configuration_id = Column<string>.FK();

        public readonly Column<string?> vc_external_reference = Column<string?>.Text(nullable: true);
        public readonly Column<string?> c_email_process_id = Column<string?>.Text(size: 36, nullable: true);

        public readonly Column<string> vc_sender_email = Column<string>.Text(size: 2048);
        public readonly Column<string> vc_sender_name = Column<string>.Text();
        public readonly Column<string> vc_destination_email = Column<string>.Text(size: 2048);
        public readonly Column<string> vc_destination_name = Column<string>.Text();

        public readonly Column<string> vc_subject = Column<string>.Text();
        public readonly Column<string> vc_message = Column<string>.Text(size: 0);

        public readonly Column<string?> vc_last_error = Column<string?>.Text(size: 0, nullable: true);

        public readonly Column<int?> i_retries = new();

        public readonly Column<string> c_emailstatus_id = Column<string>.EmbedStatus(nameof(EmailStatus));

        public readonly Column<string> vc_json_destination_and_tags = Column<string>.Text(size: 0, fake: true);
        public readonly Column<string> c_email_template_id = Column<string>.FK(fake: true);


        public readonly ViewDefinition emq_brwStandard = new(nameof(c_email_queue_id));

        public readonly ProcedureDefinition emq_qryGetQueuedItems = new();
        public readonly ProcedureDefinition emq_SubmitToQueue = new(
            nameof(c_email_configuration_id),
            nameof(vc_sender_name),
            nameof(vc_sender_email),
            nameof(vc_subject),
            nameof(vc_message),
            nameof(vc_json_destination_and_tags),
            nameof(webusr)
            );

        public readonly ProcedureDefinition emq_SubmitEmailTemplate = new(
            nameof(c_email_configuration_id),
            nameof(c_email_template_id),
            nameof(vc_json_destination_and_tags),
            nameof(webusr)
            );

        public readonly EntityForeignKey<EmailServiceConfiguration, EmailServiceQueue> FKConfiguration = new();

        public readonly EntityIndex IDXProcessId = new(keys: [nameof(c_email_process_id)]);
        public readonly EntityIndex IDXProcessReferenceId = new(keys: [nameof(c_email_process_id), nameof(vc_external_reference)]);
    }

    public class EmailServiceQueue : Entity<EmailServiceQueueDef>
    {
        public EmailServiceQueue() : base() { }
        public EmailServiceQueue(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
