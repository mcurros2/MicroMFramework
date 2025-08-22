namespace MicroM.DataDictionary.Configuration
{
    public class CategoryValuesDefinition 
    {
        public CategoryValuesDefinition() : base() { }

        public string CategoryValueID { get; internal set; } = "";
        public string Description { get; init; } = "";

        public CategoryValuesDefinition(string description, string value_id = "") : base()
        {
            CategoryValueID = value_id;
            Description = description;
        }
    }
}
