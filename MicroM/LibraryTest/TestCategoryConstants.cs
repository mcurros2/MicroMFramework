using MicroM.DataDictionary.Configuration;

namespace LibraryTest.DataDictionary.CategoriesData
{
    public class QueueType : CategoryDefinition
    {
        public QueueType() : base("Types of Queue") { }

        public readonly CategoryValuesDefinition VALUE1 = new("FIFO");
        public readonly CategoryValuesDefinition VALUE2 = new("LIFO");
        public readonly CategoryValuesDefinition VALUE3 = new("PARALLEL");

    }
}
