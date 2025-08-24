namespace MicroM.Configuration
{
    /// <summary>
    /// Flags describing which route operations are permitted for an entity.
    /// The values map to common CRUD operations as well as framework features
    /// such as lookups, views, procedures and imports. Flags can be combined to
    /// express the complete set of permissions granted to a user or group and
    /// are used by the security service when evaluating a request.
    /// </summary>
    [Flags]
    public enum AllowedRouteFlags : ushort
    {
        /// <summary>No route is allowed.</summary>
        None = 0,
        /// <summary>Allows inserting new records into the entity.</summary>
        Insert = 1,
        /// <summary>Allows updating existing records.</summary>
        Update = 2,
        /// <summary>Allows deleting records.</summary>
        Delete = 4,
        /// <summary>Allows retrieving a single record.</summary>
        Get = 8,
        /// <summary>Allows the default lookup operation for listing data.</summary>
        DefaultLookup = 16,
        /// <summary>Convenience combination of <see cref="Insert"/>, <see cref="Update"/>, <see cref="Delete"/> and <see cref="Get"/>.</summary>
        Edit = Insert | Update | Delete | Get,
        /// <summary>Allows executing custom lookup procedures.</summary>
        CustomLookup = 32,
        /// <summary>Allows access to views.</summary>
        Views = 64,
        /// <summary>Allows executing procedures.</summary>
        Procs = 128,
        /// <summary>Allows triggering custom actions.</summary>
        Actions = 256,
        /// <summary>Allows importing data for the entity.</summary>
        Import = 512,
        /// <summary>Shortcut for all flags except <see cref="Import"/>.</summary>
        All = 511,
        /// <summary>Shortcut for all flags including <see cref="Import"/>.</summary>
        AllWithImport = 1023,
    }

}

