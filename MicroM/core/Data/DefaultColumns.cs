using MicroM.Web.Authentication;
using System.Data;

namespace MicroM.Data
{
    internal class DefaultColumns
    {
        public readonly Column<DateTime> dt_inserttime = new(sql_type: SqlDbType.DateTime, column_flags: ColumnFlags.None);
        public readonly Column<DateTime> dt_lu = new(sql_type: SqlDbType.DateTime, column_flags: ColumnFlags.Insert | ColumnFlags.Update);
        public readonly Column<string> vc_webinsuser = Column<string>.Text(column_flags: ColumnFlags.None);
        public readonly Column<string> vc_webluuser = Column<string>.Text(column_flags: ColumnFlags.None);
        public readonly Column<string> vc_insuser = Column<string>.Text(column_flags: ColumnFlags.None);
        public readonly Column<string> vc_luuser = Column<string>.Text(column_flags: ColumnFlags.None);
        public readonly Column<string> webusr = Column<string>.Text(column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Fake, override_with: nameof(MicroMServerClaimTypes.MicroMUsername), value: "");

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
