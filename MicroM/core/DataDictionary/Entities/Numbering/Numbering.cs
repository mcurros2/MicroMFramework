using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

/* MMC: This class is used in replace of a sequence in SQL Server 2022 
 * because of the following, extracted from the documentation 
 * 
 * Sequence numbers are generated outside the scope of the current transaction. 
 * They are consumed whether the transaction using the sequence number is committed or rolled back. 
 * Duplicate validation only occurs once a record is fully populated. 
 * This can result in some cases where the same number is used for more than one record during creation, but then gets identified as a duplicate. 
 * If this occurs and other autonumber values have been applied to subsequent records, this can result in a gap between autonumber values and is expected behavior.
 * 
 * This class allows to update multiple rows by using the value obtained in combination with row_number when inserting multiple records
 * and just update the last value to the number (which you must do manually)
 * 
*/

namespace MicroM.DataDictionary;

/// <summary>
/// Definition for generating sequential numbers without relying on SQL Server sequences.
/// </summary>
public class NumberingDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NumberingDef"/> class.
    /// </summary>
    public NumberingDef() : base("num", nameof(Numbering)) { }

    /// <summary>
    /// Identifier of the object being numbered.
    /// </summary>
    public readonly Column<string> c_object_id = Column<string>.PK();

    /// <summary>
    /// Last number generated for the object.
    /// </summary>
    public readonly Column<long> bi_lastnumber = new();

    /// <summary>
    /// Default browse view keyed by <see cref="c_object_id"/>.
    /// </summary>
    public readonly ViewDefinition num_brwStandard = new(nameof(c_object_id));

    /// <summary>
    /// Foreign key reference to <see cref="Objects"/>.
    /// </summary>
    public readonly EntityForeignKey<Objects, Numbering> FKObjects = new();
}

/// <summary>
/// Runtime entity for retrieving and updating sequential numbers.
/// </summary>
public class Numbering : Entity<NumberingDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Numbering"/> class.
    /// </summary>
    public Numbering() : base() { }

    /// <summary>
    /// Initializes a new instance with a database client and optional encryptor.
    /// </summary>
    /// <param name="ec">Database client used for persistence.</param>
    /// <param name="encryptor">Optional encryptor for sensitive data.</param>
    public Numbering(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
}
