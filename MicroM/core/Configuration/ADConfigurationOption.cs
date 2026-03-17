namespace MicroM.Configuration;

public class ADConfigurationOption
{
    public string ADConfigurationID { get; set; } = "";
    public string ApplicationID { get; set; } = "";
    public string ADDomain { get; set; } = "";
    public string ADUserPrincipalDomain { get; set; } = "";
    public string ADContainer { get; set; } = "";
    public string ADServerIP { get; set; } = "";
    public string ADUser { get; set; } = "";
    public string ADPassword { get; set; } = "";
    public bool CreateUserOnLogin { get; set; } = false;
    public string? DefaultUserGroupID { get; set; } = null;
}
