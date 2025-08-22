namespace MicroM.Web.Authentication
{
    public interface IDeviceIdService
    {
        /// <summary>
        /// Gets the device ID.
        /// </summary>
        /// <returns></returns>
        public (string device_id, string? ipaddress, string? user_agent) GetDeviceID(string local_device_id);
    }

}
