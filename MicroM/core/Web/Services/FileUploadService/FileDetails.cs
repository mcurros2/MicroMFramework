namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the FileDetails.
    /// </summary>
    public class FileDetails
    {
        /// <summary>
        /// Gets or sets the "";.
        /// </summary>
        public string c_file_id { get; set; } = "";
        /// <summary>
        /// Gets or sets the "";.
        /// </summary>
        public string c_fileprocess_id { get; set; } = "";
        /// <summary>
        /// Gets or sets the "";.
        /// </summary>
        public string vc_filename { get; set; } = "";
        /// <summary>
        /// Gets or sets the "";.
        /// </summary>
        public string vc_filefolder { get; init; } = "";
        /// <summary>
        /// Gets or sets the "";.
        /// </summary>
        public string vc_fileguid { get; set; } = "";
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public long bi_filesize { get; set; }
        /// <summary>
        /// Gets or sets the "";.
        /// </summary>
        public string c_fileuploadstatus_id { get; set; } = "";
    }
}
