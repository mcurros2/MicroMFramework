using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

/// <summary>
/// Entity definition that links objects to categories.
/// </summary>
public class ObjectsCategoriesDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectsCategoriesDef"/> class.
    /// </summary>
    public ObjectsCategoriesDef() : base("oca", nameof(ObjectsCategories)) { }

    /// <summary>
    /// Identifier of the object.
    /// </summary>
    public readonly Column<string> c_object_id = Column<string>.PK();
    /// <summary>
    /// Identifier of the category.
    /// </summary>
    public readonly Column<string> c_category_id = Column<string>.PK();

    /// <summary>
    /// Standard browse view for object category mappings.
    /// </summary>
    public readonly ViewDefinition oca_brwStandard = new(nameof(c_object_id), nameof(c_category_id));

    /// <summary>
    /// Foreign key relationship to objects.
    /// </summary>
    public readonly EntityForeignKey<Objects, ObjectsCategories> FKObjects = new();
    /// <summary>
    /// Foreign key relationship to categories.
    /// </summary>
    public readonly EntityForeignKey<Categories, ObjectsCategories> FKCategories = new();

}

/// <summary>
/// Represents the relationship between objects and categories.
/// </summary>
public class ObjectsCategories : Entity<ObjectsCategoriesDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectsCategories"/> class.
    /// </summary>
    public ObjectsCategories() : base() { }
    /// <summary>
    /// Initializes a new instance using the specified entity client and optional encryptor.
    /// </summary>
    public ObjectsCategories(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
