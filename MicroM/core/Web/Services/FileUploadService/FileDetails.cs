namespace MicroM.Web.Services
{
    /// <summary>
    /// Metadata describing a stored file.
    /// </summary>
    public class FileDetails
    {
        /// <summary>
        /// Unique identifier of the file record.
        /// </summary>
        public string c_file_id { get; set; } = "";
        /// <summary>
        /// Identifier of the file process that produced the file.
        /// </summary>
        public string c_fileprocess_id { get; set; } = "";
        /// <summary>
        /// Original file name provided by the uploader.
        /// </summary>
        public string vc_filename { get; set; } = "";
        /// <summary>
        /// Relative folder where the file is stored.
        /// </summary>
        public string vc_filefolder { get; init; } = "";
        /// <summary>
        /// Generated unique file name used for storage.
        /// </summary>
        public string vc_fileguid { get; set; } = "";
        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long bi_filesize { get; set; }
        /// <summary>
        /// Current upload status identifier.
        /// </summary>
        public string c_fileuploadstatus_id { get; set; } = "";
    }
}
