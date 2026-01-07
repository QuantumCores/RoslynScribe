using RoslynScribe.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoslynScribe.Domain.Extensions
{
    internal class GuidesOverridesParser
    {
        internal static ScribeGuides Apply(Dictionary<string, string> overrides, ScribeGuides guide, MethodContext context)
        {
            if (overrides == null || overrides.Count == 0)
            {
                return guide;
            }

            foreach (var overrideItem in overrides)
            {
                ApplyGuideValue(overrideItem.Key, overrideItem.Value, guide, context);
            }

            return guide;
        }

        internal static string Parse(string value, MethodContext context)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (context == null)
            {
                return value;
            }

            var sb = new StringBuilder(value.Length);
            var changed = false;

            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != '{')
                {
                    sb.Append(value[i]);
                    continue;
                }

                var endIndex = value.IndexOf('}', i + 1);
                if (endIndex <= i)
                {
                    // No closing brace; keep as-is
                    sb.Append(value[i]);
                    continue;
                }

                var token = value.Substring(i + 1, endIndex - i - 1);
                string replacement = GetReplacement(context, token);

                if (replacement != null)
                {
                    sb.Append(replacement);
                    changed = true;
                }
                else
                {
                    // Unknown token; keep the original token including braces
                    sb.Append(value.Substring(i, endIndex - i + 1));
                }

                // Skip past the token
                i = endIndex;
            }

            return changed ? sb.ToString() : value;
        }

        private static string GetReplacement(MethodContext context, string token)
        {
            string replacement = null;
            if (token == nameof(MethodContext.ContainingType))
            {
                replacement = context.ContainingType;
            }

            if (token == nameof(MethodContext.ContainingTypeName))
            {
                replacement = context.ContainingTypeName;
            }

            if (token.StartsWith(nameof(MethodContext.ContainingTypeAttributes)))
            {
                replacement = GetDictionaryValue(context.ContainingTypeAttributes, token);
            }

            if (token.StartsWith(nameof(MethodContext.ContainingTypeGenericParameters)))
            {
                replacement = GetArrayValue(context.ContainingTypeGenericParameters, token);
            }

            if (token == nameof(MethodContext.MethodName))
            {
                replacement = context.MethodName;
            }

            if (token == nameof(MethodContext.MethodIdentifier))
            {
                replacement = context.MethodIdentifier;
            }

            if (token.StartsWith(nameof(MethodContext.MethodParametersTypes)))
            {
                replacement = GetArrayValue(context.MethodParametersTypes, token);
            }

            if (token.StartsWith(nameof(MethodContext.MethodArgumentsTypes)))
            {
                replacement = GetArrayValue(context.MethodArgumentsTypes, token);
            }

            if (token.StartsWith(nameof(MethodContext.MethodAttributes)))
            {
                replacement = GetDictionaryValue(context.MethodAttributes, token);
            }           

            return replacement;
        }

        private static string GetArrayValue(string[] array, string token)
        {
            var key = GetCollectionKey(token);
            if (key == null)
            {
                return string.Join("|", array ?? Array.Empty<string>());
            }

            if (int.TryParse(key, out var idx))
            {
                return array[idx];
            }
            
            ScribeConsole.Console.WriteLine($"Guides overrides could not find value in {nameof(MethodContext.MethodAttributes)} for {key}", ConsoleColor.Yellow);

            return null;
        }

        private static string GetDictionaryValue(Dictionary<string, string> dict, string token)
        {
            var key = GetCollectionKey(token);
            if (key == null)
            {
                return string.Join("|", dict.Values.ToArray() ?? Array.Empty<string>());
            }

            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }            

            ScribeConsole.Console.WriteLine($"Guides overrides could not find value in {nameof(MethodContext.MethodAttributes)} for {key}", ConsoleColor.Yellow);            

            return null;
        }


        private static string GetCollectionKey(string text)
        {
            int startIndex = text.IndexOf('[');
            int endIndex = text.IndexOf("]");
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return text.Substring(startIndex + 1, endIndex - startIndex - 1);
            }

            return null;
        }

        private static void ApplyGuideValue(string key, string value, ScribeGuides guide, MethodContext info)
        {
            if (key.Equals(ScribeGuidesTokens.Description, StringComparison.OrdinalIgnoreCase) ||
                key.Equals(nameof(ScribeGuidesTokens.Description), StringComparison.OrdinalIgnoreCase))
            {
                guide.Description = Parse(value, info);
                return;
            }

            if (key.Equals(ScribeGuidesTokens.Text, StringComparison.OrdinalIgnoreCase) ||
                key.Equals(nameof(ScribeGuidesTokens.Text), StringComparison.OrdinalIgnoreCase))
            {
                guide.Text = Parse(value, info);
                return;
            }

            if (key.Equals(ScribeGuidesTokens.Tags, StringComparison.OrdinalIgnoreCase) ||
                key.Equals(nameof(ScribeGuidesTokens.Tags), StringComparison.OrdinalIgnoreCase))
            {
                guide.Tags = Parse(value, info).Split(';');
                return;
            }

            if (key.Equals(ScribeGuidesTokens.UserDefinedId, StringComparison.OrdinalIgnoreCase) ||
                key.Equals(nameof(ScribeGuidesTokens.UserDefinedId), StringComparison.OrdinalIgnoreCase))
            {
                guide.UserDefinedId = Parse(value, info);
                return;
            }

            if (key.Equals(ScribeGuidesTokens.Path, StringComparison.OrdinalIgnoreCase) ||
                key.Equals(nameof(ScribeGuidesTokens.Path), StringComparison.OrdinalIgnoreCase))
            {
                guide.Path = Parse(value, info);
                return;
            }

            //if (key.Equals(ScribeGuidesTokens.OriginUserIds, StringComparison.OrdinalIgnoreCase) ||
            //    key.Equals(nameof(ScribeGuidesTokens.OriginUserIds), StringComparison.OrdinalIgnoreCase))
            //{
            //    guide.OriginUserIds = Parse(value, info).Split(';');
            //    return;
            //}

            if (key.Equals(ScribeGuidesTokens.DestinationUserIds, StringComparison.OrdinalIgnoreCase) ||
                key.Equals(nameof(ScribeGuidesTokens.DestinationUserIds), StringComparison.OrdinalIgnoreCase))
            {
                guide.DestinationUserIds = Parse(value, info).Split(';');
                return;
            }

            ScribeConsole.Console.WriteLine($"Could not find comment key: {key} with value: {value}", ConsoleColor.Yellow);
        }
    }
}
