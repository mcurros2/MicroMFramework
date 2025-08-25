using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Schema definition mapping users to their groups.
    /// </summary>
    public class MicromUsersGroupsMembersDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersGroupsMembersDef"/> class.
        /// </summary>
        public MicromUsersGroupsMembersDef() : base("mgm", nameof(MicromUsersGroupsMembers)) { }

        /// <summary>User group identifier.</summary>
        public readonly Column<string> c_user_group_id = Column<string>.PK();
        /// <summary>User identifier.</summary>
        public readonly Column<string> c_user_id = Column<string>.PK();

        /// <summary>Default browse view definition.</summary>
        public readonly ViewDefinition mgm_brwStandard = new(nameof(c_user_group_id), nameof(c_user_id));

        /// <summary>Relationship to users.</summary>
        public readonly EntityForeignKey<MicromUsers, MicromUsersGroupsMembers> FKMicromUsers = new();
        /// <summary>Relationship to groups.</summary>
        public readonly EntityForeignKey<MicromUsersGroups, MicromUsersGroupsMembers> FKGroups = new();

    }

    /// <summary>
    /// Entity for managing user-to-group mappings.
    /// </summary>
    public class MicromUsersGroupsMembers : Entity<MicromUsersGroupsMembersDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersGroupsMembers"/> class.
        /// </summary>
        public MicromUsersGroupsMembers() : base() { }
        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="encryptor">Optional encryptor.</param>
        public MicromUsersGroupsMembers(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}

