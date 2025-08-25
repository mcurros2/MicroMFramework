using MicroM.DataDictionary;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the IEmailService.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Performs the QueueEmail operation.
        /// </summary>
        Task<List<SubmitToQueueResult>> QueueEmail(string app_id, EmailServiceItem send_item, CancellationToken ct, bool start_processing_queue = false);

        /// <summary>
        /// Performs the StartProcessingQueue operation.
        /// </summary>
        Task StartProcessingQueue(string app_id, CancellationToken ct);
    }
}
