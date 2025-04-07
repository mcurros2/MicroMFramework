namespace MicroM.Data
{
    public class BaseColumnMapping
    {
        public readonly string ParentColName;
        public readonly string ChildColName;

        public BaseColumnMapping(string parentColName, string childColName)
        {
            ParentColName = parentColName;
            ChildColName = childColName;
        }
    }
}
