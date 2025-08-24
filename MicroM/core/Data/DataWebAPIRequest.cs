using System.Collections.Generic;

namespace MicroM.Data
{
    // MMC: I gave up trying to configure a default way to deserialize json strings
    // this data structure should be small in size and the conversion/checks should be done at the EntityColumn level
    // which has the metadata to convert the string to a correct value or throw an error
    /// <summary>
    /// Represents a request payload for data operations through the Web API.
    /// </summary>
    public class DataWebAPIRequest
    {
        /// <summary>
        /// Key values of parent entities involved in the operation.
        /// </summary>
        public Dictionary<string, object>? ParentKeys { get; set; }

        /// <summary>
        /// Values for the entity being operated on.
        /// </summary>
        public Dictionary<string, object> Values { get; set; }

        /// <summary>
        /// Records selected for processing.
        /// </summary>
        public List<Dictionary<string, object>> RecordsSelection { get; set; }

        /// <summary>
        /// Optional server claim values associated with the request.
        /// </summary>
        public Dictionary<string, object>? ServerClaims;

        /// <summary>
        /// Initializes a new instance of <see cref="DataWebAPIRequest"/> with empty collections.
        /// </summary>
        public DataWebAPIRequest()
        {
            ParentKeys = [];
            Values = [];
            RecordsSelection = [];
        }
    }
}
