using MicroM.DataDictionary;

namespace MicroM.Web.Services
{
    public interface IEmailService
    {
        Task<List<SubmitToQueueResult>> QueueEmail(string app_id, EmailServiceItem send_item, CancellationToken ct, bool start_processing_queue = false);

        Task StartProcessingQueue(string app_id, CancellationToken ct);
    }
}
