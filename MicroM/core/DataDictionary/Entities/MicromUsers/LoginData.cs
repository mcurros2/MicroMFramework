namespace MicroM.DataDictionary.Entities.MicromUsers
{
    public record LoginData
    {
        public string user_id { get; set; } = "";
        public bool locked { get; set; } = true;
        public string pwhash { get; set; } = "";
        public int badlogonattempts { get; set; } = 0;
        public int locked_minutes_remaining { get; set; } = 0;
        public string? email { get; set; }
        public string username { get; set; } = "";
        public bool disabled { get; set; } = true;
        public string? refresh_token { get; set; }
        public bool refresh_expired { get; set; } = true;
        public string? usertype_id { get; set; }
        public string? usertype_name { get; set; }
        // This will be a json string array of strings
        public string? user_groups { get; set; }
    }
}
