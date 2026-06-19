using MicroM.DataDictionary.Entities;

namespace MicroM.Web.Services;

public record FileUploadQueueFileResult(FileDetails details, NewFileNameResult new_file_result, FileStore file_store);
