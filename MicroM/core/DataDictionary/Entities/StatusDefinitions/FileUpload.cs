using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.StatusDefs
{
    public class FileUpload : StatusDefinition
    {
        public FileUpload() : base("File Upload Status") { }

        public readonly StatusValuesDefinition Pending = new("Pending", true);
        public readonly StatusValuesDefinition Uploading = new("Uploading");
        public readonly StatusValuesDefinition Uploaded = new("Uploaded");
        public readonly StatusValuesDefinition Failed = new("Failed");
        public readonly StatusValuesDefinition Cancelled = new("Cancelled");

    }
}
