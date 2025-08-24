namespace MicroM.Data
{
    // MMC: I gave up trying to configure a default way to deserialize json strings
    // this data structure should be small in size and the conversion/checks should be done at the EntityColumn level
    // which has the metadata to convert the string to a correct value or throw an error
    public class DataWebAPIRequest
    {
        public Dictionary<string, object>? ParentKeys { get; set; }
        public Dictionary<string, object> Values { get; set; }
        public List<Dictionary<string, object>> RecordsSelection { get; set; }

        public Dictionary<string, object>? ServerClaims;

        public DataWebAPIRequest()
        {
            ParentKeys = [];
            Values = [];
            RecordsSelection = [];
        }

    }

}
