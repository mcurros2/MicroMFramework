using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.StatusDefs
{
    /// <summary>
    /// Status values representing the lifecycle of a file upload.
    /// </summary>
    public class FileUpload : StatusDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileUpload"/> class.
        /// </summary>
        public FileUpload() : base("File Upload Status") { }

        /// <summary>Upload is queued but not started.</summary>
        public readonly StatusValuesDefinition Pending = new("Pending", true);

        /// <summary>Data is currently being transferred.</summary>
        public readonly StatusValuesDefinition Uploading = new("Uploading");

        /// <summary>Upload completed successfully.</summary>
        public readonly StatusValuesDefinition Uploaded = new("Uploaded");

        /// <summary>Upload failed due to an error.</summary>
        public readonly StatusValuesDefinition Failed = new("Failed");

        /// <summary>Upload was cancelled before completion.</summary>
        public readonly StatusValuesDefinition Cancelled = new("Cancelled");
    }
}
