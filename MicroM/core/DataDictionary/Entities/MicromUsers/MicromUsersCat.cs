using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Schema definition linking users to category values.
    /// </summary>
    public class MicromUsersCatDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersCatDef"/> class.
        /// </summary>
        public MicromUsersCatDef() : base("usrc", nameof(MicromUsersCat)) { }

        /// <summary>User identifier.</summary>
        public readonly Column<string> c_user_id = Column<string>.PK();
        /// <summary>Category identifier.</summary>
        public readonly Column<string> c_category_id = Column<string>.PK();
        /// <summary>Category value identifier.</summary>
        public readonly Column<string> c_categoryvalue_id = Column<string>.FK();

        /// <summary>Default browse view definition.</summary>
        public ViewDefinition usrc_brwStandard { get; private set; } = new(nameof(c_user_id));

        /// <summary>Relationship to the owning user.</summary>
        public readonly EntityForeignKey<MicromUsers, MicromUsersCat> FKMicromUsers = new();
        /// <summary>Relationship to category metadata.</summary>
        public readonly EntityForeignKey<CategoriesValues, MicromUsersCat> FKCategories = new();

    }

    /// <summary>
    /// Entity for managing user category assignments.
    /// </summary>
    public class MicromUsersCat : Entity<MicromUsersCatDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersCat"/> class.
        /// </summary>
        public MicromUsersCat() : base() { }
        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="encryptor">Optional encryptor.</param>
        public MicromUsersCat(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}

