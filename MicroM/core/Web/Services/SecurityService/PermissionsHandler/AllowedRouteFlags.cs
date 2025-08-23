namespace MicroM.Configuration
{
    /// <summary>
    /// Flags describing which route operations are permitted for an entity.
    /// These values are combined when building security records to determine
    /// the routes that a group or user may access.
    /// </summary>
    [Flags]
    public enum AllowedRouteFlags : ushort
    {
        /// <summary>No route is allowed.</summary>
        None = 0,
        /// <summary>Allows inserting an entity.</summary>
        Insert = 1,
        /// <summary>Allows updating an entity.</summary>
        Update = 2,
        /// <summary>Allows deleting an entity.</summary>
        Delete = 4,
        /// <summary>Allows retrieving an entity.</summary>
        Get = 8,
        /// <summary>Allows the default lookup operation.</summary>
        DefaultLookup = 16,
        /// <summary>Combination of <see cref="Insert"/>, <see cref="Update"/>, <see cref="Delete"/> and <see cref="Get"/>.</summary>
        Edit = Insert | Update | Delete | Get,
        /// <summary>Allows custom lookup procedures.</summary>
        CustomLookup = 32,
        /// <summary>Allows access to views.</summary>
        Views = 64,
        /// <summary>Allows executing procedures.</summary>
        Procs = 128,
        /// <summary>Allows triggering custom actions.</summary>
        Actions = 256,
        /// <summary>Allows importing data.</summary>
        Import = 512,
        /// <summary>All flags except <see cref="Import"/>.</summary>
        All = 511,
        /// <summary>All flags including <see cref="Import"/>.</summary>
        AllWithImport = 1023,
    }

}

