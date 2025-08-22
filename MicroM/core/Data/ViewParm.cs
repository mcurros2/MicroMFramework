namespace MicroM.Data
{
    public class ViewParm
    {
        public readonly int ColumnMapping;
        public readonly string CompoundGroup;
        public readonly int CompoundPosition;
        public readonly bool CompoundKey;
        public readonly bool BrowsingKey;

        public readonly ColumnBase Column;

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
