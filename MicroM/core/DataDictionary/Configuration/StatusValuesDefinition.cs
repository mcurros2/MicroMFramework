namespace MicroM.DataDictionary.Configuration
{
    public class StatusValuesDefinition
    {
        public string StatusValueID { get; internal set; } = "";
        public string Description { get; init; } = "";
        public bool InitialValue { get; init; }

        public StatusValuesDefinition() : base() { }
        public StatusValuesDefinition(string description, bool initialValue = false, string value_id = "")
        {
            StatusValueID = value_id;
            Description = description;
            InitialValue = initialValue;
        }
    }

}
