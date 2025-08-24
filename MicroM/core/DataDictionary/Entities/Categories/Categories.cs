using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

/// <summary>
/// Defines the schema for category records.
/// </summary>
public class CategoriesDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesDef"/> class.
    /// </summary>
    public CategoriesDef() : base("cat", nameof(Categories)) { }

    /// <summary>
    /// Primary key column for the category identifier.
    /// </summary>
    public readonly Column<string> c_category_id = Column<string>.PK();

    /// <summary>
    /// Descriptive name for the category.
    /// </summary>
    public readonly Column<string> vc_description = Column<string>.Text();

    /// <summary>
    /// Standard browse view keyed by category ID.
    /// </summary>
    public readonly ViewDefinition cat_brwStandard = new(nameof(c_category_id));

}

/// <summary>
/// Entity wrapper for working with <see cref="CategoriesDef"/> records.
/// </summary>
public class Categories : Entity<CategoriesDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Categories"/> class.
    /// </summary>
    public Categories() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Categories"/> class using the specified client and encryptor.
    /// </summary>
    /// <param name="ec">Entity client used for database access.</param>
    /// <param name="encryptor">Optional encryptor for sensitive data.</param>
    public Categories(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
