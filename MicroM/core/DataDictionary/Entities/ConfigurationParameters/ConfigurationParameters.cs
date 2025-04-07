using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary.Entities
{
    public class ConfigurationParametersDef : EntityDefinition
    {
        public ConfigurationParametersDef() : base("cfp", nameof(ConfigurationParameters)) { }

        public readonly Column<string> c_configuration_id = Column<string>.PK();
        public readonly Column<string> c_parameter_id = Column<string>.PK();
        public readonly Column<string> vc_value = new(sql_type: SqlDbType.VarChar);

        public ViewDefinition cfp_brwStandard { get; private set; } = new(nameof(c_configuration_id), nameof(c_parameter_id));

    }

    public class ConfigurationParameters : Entity<ConfigurationParametersDef>
    {
        public ConfigurationParameters() : base() { }

        public ConfigurationParameters(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
    }

}
