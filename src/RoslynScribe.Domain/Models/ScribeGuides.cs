using System.Text.Json.Serialization;

namespace RoslynScribe.Domain.Models
{
    public class ScribeGuides
    {
        /// <summary>
        /// Unique node identifier
        /// </summary>
        [JsonPropertyName("I")]
        public string Identifier { get; set; }

        /// <summary>
        /// Describes the maximum level of details for which this node should be printed. 
        /// E.g. if level 2 is printed then only nodes of level 1 and 2 are included
        /// </summary>
        [JsonPropertyName("L")]
        public int Level { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("P")]
        public string Path { get; set; }

        /// <summary>
        /// This property contains node ids which this node should be pointing
        /// </summary>
        [JsonPropertyName("F")]
        public string[] Followers { get; set; }

        public static ScribeGuides Default()
        {
            return new ScribeGuides()
            {
                Level = 0,
            };
        }
    }
}
