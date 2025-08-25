using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Definition for view parameters associated with stored procedures.
    /// </summary>
    public class ViewParmsDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewParmsDef"/> class.
        /// </summary>
        public ViewParmsDef() : base("vip", nameof(ViewParms)) { }

        /// <summary>
        /// Identifier of the object that owns the view.
        /// </summary>
        public readonly Column<string> c_object_id = Column<string>.PK();

        /// <summary>
        /// Identifier of the procedure.
        /// </summary>
        public readonly Column<int> c_proc_id = Column<int>.PK();

        /// <summary>
        /// Primary key for the view parameter.
        /// </summary>
        public readonly Column<int> c_viewparm_id = Column<int>.PK(autonum: true);

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public readonly Column<string> vc_parmname = new(sql_type: SqlDbType.VarChar, size: 255);

        /// <summary>
        /// Column mapping identifier.
        /// </summary>
        public readonly Column<int?> i_columnmapping = new();

        /// <summary>
        /// Compound group name.
        /// </summary>
        public readonly Column<string?> vc_compoundgroup = new(sql_type: SqlDbType.VarChar, size: 80, nullable: true);

        /// <summary>
        /// Position within the compound group.
        /// </summary>
        public readonly Column<int?> i_compoundposition = new();

        /// <summary>
        /// Indicates whether the parameter is part of a compound key.
        /// </summary>
        public readonly Column<bool> bt_compoundkey = new();

        /// <summary>
        /// Indicates whether the parameter is used for browsing.
        /// </summary>
        public readonly Column<bool> bt_browsingkey = new();

        /// <summary>
        /// Default browse view for view parameters.
        /// </summary>
        public ViewDefinition vip_brwStandard { get; private set; } = new(nameof(c_object_id), nameof(c_proc_id), nameof(c_viewparm_id));

        /// <summary>
        /// Relationship to the related <see cref="Procs"/> record.
        /// </summary>
        public readonly EntityForeignKey<Procs, ViewParms> FKObjects = new();

    }

    /// <summary>
    /// Entity for working with view parameter records.
    /// </summary>
    public class ViewParms : Entity<ViewParmsDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewParms"/> class.
        /// </summary>
        public ViewParms() : base() { }

        /// <summary>
        /// Initializes a new instance using the specified client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client for database access.</param>
        /// <param name="encryptor">Optional encryptor for sensitive data.</param>
        public ViewParms(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
