namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Defines a contract for obtaining a device identifier used during authentication.
    /// </summary>
    public interface IDeviceIdService
    {
        /// <summary>
        /// Generates a device identifier for the current request.
        /// </summary>
        /// <param name="local_device_id">Identifier supplied by the client for correlation.</param>
        /// <returns>A tuple containing the resolved device identifier, client IP address, and user agent.</returns>
        public (string device_id, string? ipaddress, string? user_agent) GetDeviceID(string local_device_id);
    }

}
