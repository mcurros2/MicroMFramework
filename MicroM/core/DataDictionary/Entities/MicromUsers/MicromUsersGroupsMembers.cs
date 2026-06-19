using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromUsersGroupsMembersDef : EntityDefinition
{
    public MicromUsersGroupsMembersDef() : base("mgm", nameof(MicromUsersGroupsMembers)) { }

    public readonly Column<string> c_user_group_id = Column<string>.PK();
    public readonly Column<string> c_user_id = Column<string>.PK();

    public readonly ViewDefinition mgm_brwStandard = new(nameof(c_user_group_id), nameof(c_user_id));

    public readonly EntityForeignKey<MicromUsers, MicromUsersGroupsMembers> FKMicromUsers = new();
    public readonly EntityForeignKey<MicromUsersGroups, MicromUsersGroupsMembers> FKGroups = new();

}

public class MicromUsersGroupsMembers : Entity<MicromUsersGroupsMembersDef>
{
    public MicromUsersGroupsMembers() : base() { }
    public MicromUsersGroupsMembers(string? schema_name) : base(schema_name) { }
    public MicromUsersGroupsMembers(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
