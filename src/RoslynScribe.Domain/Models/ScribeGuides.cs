using System.Text.Json.Serialization;

namespace RoslynScribe.Domain.Models
{
    public class ScribeGuides
    {
        /// <summary>
        /// Unique node identifier
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.Id)]
        public string Id { get; set; }

        /// <summary>
        /// Unique user defined node identifier
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.UserDefinedId)]
        public string UserDefinedId { get; set; }

        /// <summary>
        /// Describes the maximum level of details for which this node should be printed. 
        /// E.g. if level 2 is printed then only nodes of level 1 and 2 are included
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.Level)]
        public int Level { get; set; } = 1;

        /// <summary>
        /// This text is used as presented text on the node.
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.Text)]
        public string Text { get; set; }

        /// <summary>
        /// This text is used to give more description to the node if needed.
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.Description)]
        public string Description { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.Path)]
        public string Path { get; set; }

        /// <summary>
        /// This property contains node ids that should be pointing this node
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.OriginUserIds)]
        public string[] OriginUserIds { get; set; }

        /// <summary>
        /// This property contains node ids which this node should be pointing
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.DestinationUserIds)]
        public string[] DestinationUserIds { get; set; }

        /// <summary>
        /// Tags for given node
        /// </summary>
        [JsonPropertyName(ScribeGuidesTokens.Tags)]
        public string[] Tags { get; set; }

        public static ScribeGuides Default()
        {
            return new ScribeGuides();
        }
    }
}
