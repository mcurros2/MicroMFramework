namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Defines claim type names used internally by the MicroM server.
    /// </summary>
    public class MicroMServerClaimTypes
    {
        /// <summary>
        /// Claim type for the application identifier.
        /// </summary>
        public const string MicroMAPP_id = nameof(MicroMAPP_id);

        /// <summary>
        /// Claim type for the unique user identifier.
        /// </summary>
        public const string MicroMUser_id = nameof(MicroMUser_id);

        /// <summary>
        /// Claim type for the user's name.
        /// </summary>
        public const string MicroMUsername = nameof(MicroMUsername);

        /// <summary>
        /// Claim type indicating the MicroM server handling the request.
        /// </summary>
        public const string MicroMServer = nameof(MicroMServer);

        /// <summary>
        /// Claim type for the user's password or password hash.
        /// </summary>
        public const string MicroMPassword = nameof(MicroMPassword);

        /// <summary>
        /// Claim type for the user's role or type identifier.
        /// </summary>
        public const string MicroMUserType_id = nameof(MicroMUserType_id);

        /// <summary>
        /// Claim type describing the groups to which the user belongs.
        /// </summary>
        public const string MicroMUserGroups = nameof(MicroMUserGroups);

        /// <summary>
        /// Claim type for the identifier of the user's device.
        /// </summary>
        public const string MicroMUserDeviceID = nameof(MicroMUserDeviceID);
    }
}
