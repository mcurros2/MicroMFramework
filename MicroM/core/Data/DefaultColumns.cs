namespace MicroM.Data;

internal class DefaultColumns
{
    public readonly Column<DateTime> dt_inserttime = DefaultColumnsFactory.dt_inserttime();
    public readonly Column<DateTime> dt_lu = DefaultColumnsFactory.dt_lu();
    public readonly Column<string> vc_webinsuser = DefaultColumnsFactory.vc_webinsuser();
    public readonly Column<string> vc_webluuser = DefaultColumnsFactory.vc_webluuser();
    public readonly Column<string> vc_insuser = DefaultColumnsFactory.vc_insuser();
    public readonly Column<string> vc_luuser = DefaultColumnsFactory.vc_luuser();
    public readonly Column<string> webusr = DefaultColumnsFactory.webusr();

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
