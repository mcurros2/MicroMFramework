using MicroM.DataDictionary;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the EmailHostedService.
    /// </summary>
    public class EmailHostedService : IHostedService, IEmailService, IDisposable
    {
        private static int _instanceCounter = 0;
        private CancellationTokenSource? _serviceCTS;
        private EmailService? _emailService;
        private bool disposedValue;
        private readonly ILogger<EmailHostedService> logger;
        private readonly ILogger<EmailService> emailLogger;
        private readonly IMicroMAppConfiguration app_config;
        private readonly IBackgroundTaskQueue btq;
        private readonly IMicroMEncryption encryptor;


        /// <summary>
        /// Performs the EmailHostedService operation.
        /// </summary>
        public EmailHostedService(ILogger<EmailHostedService> logger, ILogger<EmailService> emailLogger, IBackgroundTaskQueue btq, IMicroMAppConfiguration app_config, IMicroMEncryption encryptor)
        {
            _instanceCounter++;
            this.logger = logger;
            this.emailLogger = emailLogger;
            this.btq = btq;
            this.app_config = app_config;
            this.encryptor = encryptor;

            if (_instanceCounter > 1) logger.LogError("EmailHostedService found more that one instance created. Instances: {instance} ", _instanceCounter);
        }

        /// <summary>
        /// Performs the QueueEmail operation.
        /// </summary>
        public async Task<List<SubmitToQueueResult>> QueueEmail(string app_id, EmailServiceItem send_item, CancellationToken ct, bool start_processing_queue = false)
        {
            if (_emailService != null && _serviceCTS != null)
            {
                return await _emailService.QueueEmail(app_id, send_item, ct, start_processing_queue);
            }
            throw new Exception("EmailService not started.");
        }

        /// <summary>
        /// Performs the StartProcessingQueue operation.
        /// </summary>
        public async Task StartProcessingQueue(string app_id, CancellationToken ct)
        {
            if (_emailService != null && _serviceCTS != null)
            {
                await _emailService.StartProcessingQueue(app_id, _serviceCTS.Token);
                return;
            }
            throw new Exception("EmailService not started.");
        }

        /// <summary>
        /// Performs the StartAsync operation.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("EmailServiceHostedService is starting.");
            _serviceCTS = CancellationTokenSource.CreateLinkedTokenSource(btq.QueueCT, cancellationToken);
            _emailService = new EmailService(emailLogger, btq, app_config, encryptor, _serviceCTS.Token);

            // start processing the queue for all apps
            foreach (var app_id in app_config.GetAppIDs())
            {
                await _emailService.StartProcessingQueue(app_id, _serviceCTS.Token);
            }

            logger.LogInformation("EmailServiceHostedService started.");
            return;
        }

        /// <summary>
        /// Performs the StopAsync operation.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("EmailServiceHostedService is stopping.");
            _serviceCTS?.Cancel();
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _serviceCTS?.Cancel();
                    _serviceCTS?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }


        /// <summary>
        /// Performs the Dispose operation.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
