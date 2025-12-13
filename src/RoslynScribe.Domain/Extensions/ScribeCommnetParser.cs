using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Extensions
{
    internal class ScribeCommnetParser
    {
        internal static ScribeComment Parse(string[] values)
        {
            List<string> comments = new List<string>();
            string guides = null;
            foreach (var value in values)
            {

                var commentsTmp = value.Split(new string[] { ScribeAnalyzer.CommentLabel }, System.StringSplitOptions.RemoveEmptyEntries);
                if (commentsTmp.Length != 0)
                {
                    comments.AddRange(commentsTmp);
                }

                //var guidesTmp = value.Split(new string[] { ScribeAnalyzer.GuidesLabel }, System.StringSplitOptions.RemoveEmptyEntries);
                //if(guidesTmp.Length != 0)
                //{
                //    guides = guidesTmp[0];
                //}
            }

            if(guides != null)
            {
                return Parse(guides, comments.ToArray());
            }

            return new ScribeComment
            {
                Comments = comments.ToArray(),
                Guide = ScribeGuides.Default(),
            };
        }

        internal static ScribeComment Parse(string guides, string[] comments)
        {
            return new ScribeComment
            {
                Comments = comments,
                Guide = ParseGuides(guides),
            };
        }

        internal static ScribeGuides ParseGuides(string guides)
        {
            return new ScribeGuides() { };
        }
    }
}
