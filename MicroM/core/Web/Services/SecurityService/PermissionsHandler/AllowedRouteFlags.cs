namespace MicroM.Configuration
{
    [Flags]
    public enum AllowedRouteFlags : ushort
    {
        None = 0,
        Insert = 1,
        Update = 2,
        Delete = 4,
        Get = 8,
        DefaultLookup = 16,
        Edit = Insert | Update | Delete | Get,
        CustomLookup = 32,
        Views = 64,
        Procs = 128,
        Actions = 256,
        Import = 512,
        All = 511,
        AllWithImport = 1023,
    }

}
