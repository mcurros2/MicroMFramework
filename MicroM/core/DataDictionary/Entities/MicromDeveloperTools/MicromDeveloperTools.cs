using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromDeveloperToolsDef : EntityDefinition
{
    public MicromDeveloperToolsDef() : base("mdt", nameof(MicromDeveloperTools)) { Fake = true; }

}

public class MicromDeveloperTools : Entity<MicromDeveloperToolsDef>
{
    public MicromDeveloperTools() : base() { }
    public MicromDeveloperTools(string? schema_name) : base(schema_name) { }
    public MicromDeveloperTools(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }
}
