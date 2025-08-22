namespace MicroM.Data
{
    /// <summary>
    /// Represents a parameter used in a <see cref="ViewDefinition"/>.
    /// </summary>
    public class ViewParm
    {
        /// <summary>The zero-based column mapping for the parameter.</summary>
        public readonly int ColumnMapping;

        /// <summary>Group identifier for compound keys.</summary>
        public readonly string CompoundGroup;

        /// <summary>Position within a compound key group.</summary>
        public readonly int CompoundPosition;

        /// <summary>Indicates whether the parameter is part of a compound key.</summary>
        public readonly bool CompoundKey;

        /// <summary>Indicates whether the parameter is used as the browsing key.</summary>
        public readonly bool BrowsingKey;

        /// <summary>The underlying column definition.</summary>
        public readonly ColumnBase Column;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewParm"/> class.
        /// </summary>
        public ViewParm(ColumnBase column, int column_mapping = -1, string compound_group = "", int compound_position = -1, bool compound_key = false, bool browsing_key = false)
        {
            Column = column;

            CompoundGroup = compound_group;
            CompoundPosition = compound_position;
            CompoundKey = compound_key;
            ColumnMapping = column_mapping;
            BrowsingKey = browsing_key;
        }

    }
}

