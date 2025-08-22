namespace MicroM.Data
{
    /// <summary>
    /// Represents a mapping between a parent and child column.
    /// </summary>
    public class BaseColumnMapping
    {
        /// <summary>The parent column name.</summary>
        public readonly string ParentColName;

        /// <summary>The child column name.</summary>
        public readonly string ChildColName;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseColumnMapping"/> class.
        /// </summary>
        /// <param name="parentColName">Name of the parent column.</param>
        /// <param name="childColName">Name of the child column.</param>
        public BaseColumnMapping(string parentColName, string childColName)
        {
            ParentColName = parentColName;
            ChildColName = childColName;
        }
    }
}
