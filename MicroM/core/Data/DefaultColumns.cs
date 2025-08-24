using MicroM.Web.Authentication;
using System.Data;

namespace MicroM.Data
{
    /// <summary>
    /// Defines default columns automatically included in generated entities.
    /// </summary>
    internal class DefaultColumns
    {
        /// <summary>
        /// Column storing the row insertion timestamp.
        /// </summary>
        public readonly Column<DateTime> dt_inserttime = new(sql_type: SqlDbType.DateTime, column_flags: ColumnFlags.None);
        /// <summary>
        /// Column storing the last update timestamp.
        /// </summary>
        public readonly Column<DateTime> dt_lu = new(sql_type: SqlDbType.DateTime, column_flags: ColumnFlags.Insert | ColumnFlags.Update);
        /// <summary>
        /// Column storing the web user that inserted the row.
        /// </summary>
        public readonly Column<string> vc_webinsuser = Column<string>.Text(column_flags: ColumnFlags.None);
        /// <summary>
        /// Column storing the web user that last updated the row.
        /// </summary>
        public readonly Column<string> vc_webluuser = Column<string>.Text(column_flags: ColumnFlags.None);
        /// <summary>
        /// Column storing the application user that inserted the row.
        /// </summary>
        public readonly Column<string> vc_insuser = Column<string>.Text(column_flags: ColumnFlags.None);
        /// <summary>
        /// Column storing the application user that last updated the row.
        /// </summary>
        public readonly Column<string> vc_luuser = Column<string>.Text(column_flags: ColumnFlags.None);
        /// <summary>
        /// Column containing the username overridden from server claims when inserting or updating.
        /// </summary>
        public readonly Column<string> webusr = Column<string>.Text(column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Fake, override_with: nameof(MicroMServerClaimTypes.MicroMUsername), value: "");

        /// <summary>
        /// List of system column names defined in this class.
        /// </summary>
        public readonly static string[] SystemNames = {
                nameof(dt_inserttime),
                nameof(dt_lu),
                nameof(vc_webinsuser),
                nameof(vc_webluuser),
                nameof(vc_insuser),
                nameof(vc_luuser),
                nameof(webusr)
            };


    }
}
