using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary.Entities
{
    /// <summary>
    /// Definition for configuration parameters stored in the data dictionary.
    /// </summary>
    public class ConfigurationParametersDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationParametersDef"/> class.
        /// </summary>
        public ConfigurationParametersDef() : base("cfp", nameof(ConfigurationParameters)) { }

        /// <summary>
        /// Configuration identifier.
        /// </summary>
        public readonly Column<string> c_configuration_id = Column<string>.PK();

        /// <summary>
        /// Parameter identifier.
        /// </summary>
        public readonly Column<string> c_parameter_id = Column<string>.PK();

        /// <summary>
        /// Parameter value.
        /// </summary>
        public readonly Column<string> vc_value = new(sql_type: SqlDbType.VarChar);

        /// <summary>
        /// Standard browse view for configuration parameters.
        /// </summary>
        public ViewDefinition cfp_brwStandard { get; private set; } = new(nameof(c_configuration_id), nameof(c_parameter_id));

    }

    /// <summary>
    /// Runtime entity for interacting with configuration parameters.
    /// </summary>
    public class ConfigurationParameters : Entity<ConfigurationParametersDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationParameters"/> class.
        /// </summary>
        public ConfigurationParameters() : base() { }

        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        public ConfigurationParameters(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
    }

}
