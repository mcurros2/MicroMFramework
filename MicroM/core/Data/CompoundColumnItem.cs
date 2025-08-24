namespace MicroM.Data
{
    public class CompoundColumnItem
    {
        public readonly ColumnBase Column;
        public readonly int Position;
        public readonly bool CompoundKey;

        public CompoundColumnItem(ColumnBase col, int position, bool compound_key)
        {
            Column = col;
            Position = position;
            CompoundKey = compound_key;
        }
    }
}
