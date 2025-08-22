namespace MicroM.Data
{
    /// <summary>
    /// Represents a column participating in a compound key, including its position.
    /// </summary>
    public class CompoundColumnItem
    {
        /// <summary>The column definition.</summary>
        public readonly ColumnBase Column;

        /// <summary>Zero-based position of the column within the compound key.</summary>
        public readonly int Position;

        /// <summary>Indicates whether the column is part of the compound key.</summary>
        public readonly bool CompoundKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundColumnItem"/> class.
        /// </summary>
        /// <param name="col">Column definition.</param>
        /// <param name="position">Position within the compound key.</param>
        /// <param name="compound_key">Whether the column belongs to the compound key.</param>
        public CompoundColumnItem(ColumnBase col, int position, bool compound_key)
        {
            Column = col;
            Position = position;
            CompoundKey = compound_key;
        }
    }
}

