namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents the IDeviceIdService.
    /// </summary>
    public interface IDeviceIdService
    {
        /// <summary>
        /// Gets the device ID.
        /// </summary>
        /// <returns></returns>
        public (string device_id, string? ipaddress, string? user_agent) GetDeviceID(string local_device_id);
    }

}
