using MicroM.Core;
using MicroM.Data;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// This definition is used to extend any entity definition that will create a user
    /// </summary>
    public class MicromUsersStatusPanelDef(string mneo, string name, bool add_default_columns = true) : EntityDefinition(mneo, name, add_default_columns)
    {

        // MMC: for user creation
        public readonly Column<string?> vc_username = Column<string?>.Text(fake: true, nullable:true);
        public readonly Column<string?> vc_password = Column<string?>.Text(size: 2048, fake: true, nullable: true);
        public readonly Column<string[]?> vc_user_groups = Column<string[]?>.Text(size: 0, fake: true, isArray: true, nullable: true);
        public readonly Column<bool> bt_disabled = new(fake: true);

        public readonly Column<bool> bt_islocked = new(column_flags: ColumnFlags.None, fake: true);
        public readonly Column<int> i_badlogonattempts = new(value: 0, fake: true, column_flags: ColumnFlags.None);
        public readonly Column<int> i_locked_minutes_remaining = new(fake: true, column_flags: ColumnFlags.None);
        public readonly Column<DateTime?> dt_locked = new(fake: true, column_flags: ColumnFlags.None);
        public readonly Column<DateTime?> dt_last_login = new(fake: true, column_flags: ColumnFlags.None);
        public readonly Column<DateTime?> dt_last_refresh = new(fake: true, column_flags: ColumnFlags.None);

    }
}
