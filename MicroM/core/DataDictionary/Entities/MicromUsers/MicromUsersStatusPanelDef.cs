using MicroM.Core;
using MicroM.Data;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Definition that extends entities which create users with additional status information.
    /// </summary>
    public class MicromUsersStatusPanelDef(string mneo, string name, bool add_default_columns = true) : EntityDefinition(mneo, name, add_default_columns)
    {

        // MMC: for user creation
        /// <summary>Plain text username.</summary>
        public readonly Column<string?> vc_username = Column<string?>.Text(fake: true, nullable: true);
        /// <summary>Plain text password.</summary>
        public readonly Column<string?> vc_password = Column<string?>.Text(size: 2048, fake: true, nullable: true);
        /// <summary>User group identifiers.</summary>
        public readonly Column<string[]?> vc_user_groups = Column<string[]?>.Text(size: 0, fake: true, isArray: true, nullable: true);
        /// <summary>Indicates whether the user is disabled.</summary>
        public readonly Column<bool> bt_disabled = new(fake: true);

        /// <summary>Indicates whether the user is locked.</summary>
        public readonly Column<bool> bt_islocked = new(column_flags: ColumnFlags.None, fake: true);
        /// <summary>Number of failed login attempts.</summary>
        public readonly Column<int> i_badlogonattempts = new(value: 0, fake: true, column_flags: ColumnFlags.None);
        /// <summary>Minutes remaining before the user is unlocked.</summary>
        public readonly Column<int> i_locked_minutes_remaining = new(fake: true, column_flags: ColumnFlags.None);
        /// <summary>Timestamp when the user was locked.</summary>
        public readonly Column<DateTime?> dt_locked = new(fake: true, column_flags: ColumnFlags.None);
        /// <summary>Timestamp of the last successful login.</summary>
        public readonly Column<DateTime?> dt_last_login = new(fake: true, column_flags: ColumnFlags.None);
        /// <summary>Timestamp of the last refresh token.</summary>
        public readonly Column<DateTime?> dt_last_refresh = new(fake: true, column_flags: ColumnFlags.None);

    }
}

