using System.Text.Json.Serialization;

namespace RoslynScribe.Domain.Models
{
    public class ScribeGuides
    {
        /// <summary>
        /// Unique node identifier
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Unique user defined node identifier
        /// </summary>
        [JsonPropertyName("Uid")]
        public string UserDefinedId { get; set; }

        /// <summary>
        /// Describes the maximum level of details for which this node should be printed. 
        /// E.g. if level 2 is printed then only nodes of level 1 and 2 are included
        /// </summary>
        [JsonPropertyName("L")]
        public int Level { get; set; } = 1;

        /// <summary>
        /// This text is used to describe the node.
        /// </summary>
        [JsonPropertyName("T")]
        public string Text { get; set; }

        /// <summary>
        /// This text is used to describe the node.
        /// </summary>
        [JsonPropertyName("D")]
        public string Description { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("P")]
        public string Path { get; set; }

        /// <summary>
        /// This property contains node ids that should be pointing this node
        /// </summary>
        [JsonPropertyName("O")]
        public string[] OriginIds { get; set; }

        /// <summary>
        /// This property contains node ids which this node should be pointing
        /// </summary>
        [JsonPropertyName("DUI")]
        public string[] DestinationUserIds { get; set; }

        /// <summary>
        /// Tags for given node
        /// </summary>
        [JsonPropertyName("Tags")]
        public string[] Tags { get; set; }

        public static ScribeGuides Default()
        {
            return new ScribeGuides()
            {
                Level = 0,
            };
        }
    }
}
