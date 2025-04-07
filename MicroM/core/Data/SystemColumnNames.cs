namespace MicroM.Data
{
    public static class SystemColumnNames
    {
        public const string dt_inserttime = "dt_inserttime";
        public const string dt_lu = "dt_lu";
        public const string vc_webinsuser = "vc_webinsuser";
        public const string vc_webluuser = "vc_webluuser";
        public const string vc_insuser = "vc_insuser";
        public const string vc_luuser = "vc_luuser";
        public const string webusr = "webusr";

        public static string AsString => $"{dt_inserttime}, {dt_lu}, {vc_webinsuser}, {vc_webluuser}, {vc_insuser}, {vc_luuser}, {webusr}";

        private static readonly string[] _asStringArray = [dt_inserttime, dt_lu, vc_webinsuser, vc_webluuser, vc_insuser, vc_luuser, webusr];
        public static string[] AsStringArray => _asStringArray;

    }

}
