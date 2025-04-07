namespace MicroM.Web.Services
{
    public class FileDetails
    {
        public string c_file_id { get; set; } = "";
        public string c_fileprocess_id { get; set; } = "";
        public string vc_filename { get; set; } = "";
        public string vc_filefolder { get; init; } = "";
        public string vc_fileguid { get; set; } = "";
        public long bi_filesize { get; set; }
        public string c_fileuploadstatus_id { get; set; } = "";
    }
}
