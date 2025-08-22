namespace MicroM.DataDictionary.Configuration
{
    public class IDDescriptionDefinition
    {
        public string ID { get; init; } = null!;
        public string Description { get; init; } = null!;

        public IDDescriptionDefinition() { }

        public IDDescriptionDefinition(string id, string description)
        {
            ID = id;
            Description = description;
        }
    }
}
