using MicroM.Web.Authentication;

namespace MicroM.Data;

public static class DefaultColumnsFactory
{
    public static Column<string> webusr(bool delete_flag = false)
    {
        return Column<string>.Text(
            name: SystemColumnNames.webusr
            , column_flags: delete_flag ? ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Fake | ColumnFlags.Delete : ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Fake
            , override_with: nameof(MicroMServerClaimTypes.MicroMUsername),
            value: "");
    }

    public static Column<DateTime> dt_inserttime()
    {
        return new Column<DateTime>(
            name: SystemColumnNames.dt_inserttime,
            column_flags: ColumnFlags.None
            );
    }

    public static Column<DateTime> dt_lu()
    {
        return new Column<DateTime>(
            name: SystemColumnNames.dt_lu,
            column_flags: ColumnFlags.Insert | ColumnFlags.Update
            );
    }

    public static Column<string> vc_insuser()
    {
        return Column<string>.Text(
            name: SystemColumnNames.vc_insuser,
            column_flags: ColumnFlags.None);
    }

    public static Column<string> vc_luuser()
    {
        return Column<string>.Text(
            name: SystemColumnNames.vc_luuser,
            column_flags: ColumnFlags.None);
    }

    public static Column<string> vc_webinsuser()
    {
        return Column<string>.Text(
            name: SystemColumnNames.vc_webinsuser,
            column_flags: ColumnFlags.None);
    }

    public static Column<string> vc_webluuser()
    {
        return Column<string>.Text(
            name: SystemColumnNames.vc_webluuser,
            column_flags: ColumnFlags.None);
    }

}
