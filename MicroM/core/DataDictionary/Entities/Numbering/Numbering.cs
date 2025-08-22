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

public class NumberingDef : EntityDefinition
{
    public NumberingDef() : base("num", nameof(Numbering)) { }

    public readonly Column<string> c_object_id = Column<string>.PK();
    public readonly Column<long> bi_lastnumber = new();

    public readonly ViewDefinition num_brwStandard = new(nameof(c_object_id));

    public readonly EntityForeignKey<Objects, Numbering> FKObjects = new();

}

public class Numbering : Entity<NumberingDef>
{
    public Numbering() : base() { }

    public Numbering(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
