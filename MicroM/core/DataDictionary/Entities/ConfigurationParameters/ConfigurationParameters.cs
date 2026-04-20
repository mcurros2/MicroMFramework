using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ConfigurationParametersDef : EntityDefinition
{
    public ConfigurationParametersDef() : base("cfp", nameof(ConfigurationParameters)) { }

    public readonly Column<string> c_configuration_id = Column<string>.PK();
    public readonly Column<string> c_parameter_id = Column<string>.PK();
    public readonly Column<string> vc_value = Column<string>.Text();

    public readonly ViewDefinition cfp_brwStandard = new(nameof(c_configuration_id), nameof(c_parameter_id));

}

public class ConfigurationParameters : Entity<ConfigurationParametersDef>
{
    public ConfigurationParameters() : base() { }
    public ConfigurationParameters(string? schema_name) : base(schema_name) { }
    public ConfigurationParameters(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }
}
