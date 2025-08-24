using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

/// <summary>
/// Defines the schema for category value records.
/// </summary>
public class CategoriesValuesDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesValuesDef"/> class.
    /// </summary>
    public CategoriesValuesDef() : base("cav", nameof(CategoriesValues)) { }

    /// <summary>
    /// Primary key column referencing the category.
    /// </summary>
    public readonly Column<string> c_category_id = Column<string>.PK();

    /// <summary>
    /// Primary key column for the category value identifier.
    /// </summary>
    public readonly Column<string> c_categoryvalue_id = Column<string>.PK();

    /// <summary>
    /// Descriptive text for the value.
    /// </summary>
    public readonly Column<string> vc_description = Column<string>.Text();

    /// <summary>
    /// Standard browse view keyed by category and value IDs.
    /// </summary>
    public readonly ViewDefinition cav_brwStandard = new(nameof(c_category_id), nameof(c_categoryvalue_id));

    /// <summary>
    /// Foreign key to the parent <see cref="Categories"/> entity.
    /// </summary>
    public readonly EntityForeignKey<Categories, CategoriesValues> FKCategories = new();

    /// <summary>
    /// Unique constraint ensuring descriptions are unique within a category.
    /// </summary>
    public readonly EntityUniqueConstraint UNDescription = new(keys: [nameof(c_category_id), nameof(vc_description)]);

}

/// <summary>
/// Entity wrapper for working with <see cref="CategoriesValuesDef"/> records.
/// </summary>
public class CategoriesValues : Entity<CategoriesValuesDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesValues"/> class.
    /// </summary>
    public CategoriesValues() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesValues"/> class using the specified client and encryptor.
    /// </summary>
    /// <param name="ec">Entity client used for database access.</param>
    /// <param name="encryptor">Optional encryptor for sensitive data.</param>
    public CategoriesValues(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
