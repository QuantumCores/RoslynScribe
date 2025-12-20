using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RoslynScribe.Domain.Extensions
{
    internal class ScribeCommnetParser
    {
        internal static ScribeGuides Parse(string[] values)
        {
            var comments = new List<string>();
            var guide = ScribeGuides.Default();

            if (values != null)
            {
                foreach (var value in values)
                {
                    ParseCommentLine(value ?? string.Empty, guide, comments);
                }
            }

            return guide;            
        }

        private static void ParseCommentLine(string value, ScribeGuides guide, List<string> comments)
        {
            var labelIndex = value.IndexOf(ScribeAnalyzer.CommentLabel, StringComparison.Ordinal);
            if (labelIndex < 0)
            {
                var trimmed = value.Trim();
                if (trimmed.Length != 0)
                {
                    comments.Add(trimmed);
                }
                return;
            }

            var tail = value.Substring(labelIndex + ScribeAnalyzer.CommentLabel.Length);
            ParseTail(tail, guide, comments);
        }

        private static void ParseTail(string tail, ScribeGuides guide, List<string> comments)
        {
            var outside = new List<char>();

            var i = 0;
            while (i < tail.Length)
            {
                if (tail[i] != '[')
                {
                    outside.Add(tail[i]);
                    i++;
                    continue;
                }

                var outsideText = new string(outside.ToArray()).Trim();
                if (outsideText.Length != 0)
                {
                    comments.Add(outsideText);
                }
                outside.Clear();

                if (!TryReadBracketBlock(tail, ref i, out var block))
                {
                    var remaining = tail.Substring(i).Trim();
                    if (remaining.Length != 0)
                    {
                        comments.Add(remaining);
                    }
                    return;
                }

                ParseBracketBlock(block, guide, comments);
            }

            var endText = new string(outside.ToArray()).Trim();
            if (endText.Length != 0)
            {
                comments.Add(endText);
            }
        }

        private static bool TryReadBracketBlock(string text, ref int index, out string content)
        {
            // index points at '['
            var start = index + 1;
            var i = start;
            var inQuoted = false;
            var escaped = false;

            while (i < text.Length)
            {
                var c = text[i];
                if (inQuoted)
                {
                    if (escaped)
                    {
                        escaped = false;
                        i++;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        i++;
                        continue;
                    }

                    if (c == '`')
                    {
                        inQuoted = false;
                        i++;
                        continue;
                    }

                    i++;
                    continue;
                }

                if (c == '`')
                {
                    inQuoted = true;
                    i++;
                    continue;
                }

                if (c == ']')
                {
                    content = text.Substring(start, i - start);
                    index = i + 1;
                    return true;
                }

                i++;
            }

            content = null;
            return false;
        }

        private static void ParseBracketBlock(string block, ScribeGuides guide, List<string> comments)
        {
            var hadAssignment = false;
            foreach (var segment in SplitTopLevel(block, ','))
            {
                var token = segment.Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                var colonIndex = IndexOfTopLevel(token, ':');
                if (colonIndex < 0)
                {
                    comments.Add(token);
                    continue;
                }

                var key = token.Substring(0, colonIndex).Trim();
                var valuePart = token.Substring(colonIndex + 1).Trim();

                if (key.Length == 0)
                {
                    comments.Add(token);
                    continue;
                }

                hadAssignment = true;
                var value = ReadValue(valuePart, out var trailing);
                if (trailing.Length != 0)
                {
                    comments.Add(trailing);
                }

                ApplyGuideValue(guide, key, value);
            }

            if (!hadAssignment)
            {
                var trimmed = block.Trim();
                if (trimmed.Length != 0)
                {
                    comments.Add(trimmed);
                }
            }
        }

        private static void ApplyGuideValue(ScribeGuides guide, string key, string value)
        {
            if (key.Equals("D", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Description", StringComparison.OrdinalIgnoreCase))
            {
                guide.Description = value;
                return;
            }

            if (key.Equals("T", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                guide.Text = value;
                return;
            }

            if (key.Equals("Tags", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Tag", StringComparison.OrdinalIgnoreCase))
            {
                guide.Tags = MergeStringList(guide.Tags, SplitList(value));
                return;
            }

            if (key.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Identifier", StringComparison.OrdinalIgnoreCase))
            {
                guide.Id = value;
                return;
            }

            if (key.Equals("Uid", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("UserIdentifier", StringComparison.OrdinalIgnoreCase))
            {
                guide.UserDefinedId = value;
                return;
            }

            if (key.Equals("P", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Path", StringComparison.OrdinalIgnoreCase))
            {
                guide.Path = value;
                return;
            }

            if (key.Equals("L", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Level", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level))
                {
                    guide.Level = level;
                }
                return;
            }

            if (key.Equals("O", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("OriginIds", StringComparison.OrdinalIgnoreCase))
            {
                guide.OriginIds = MergeStringList(guide.OriginIds, SplitList(value));
                return;
            }

            if (key.Equals("Dui", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("DestinationUserIds", StringComparison.OrdinalIgnoreCase))
            {
                guide.DestinationUserIds = MergeStringList(guide.DestinationUserIds, SplitList(value));
                return;
            }
        }

        private static string[] SplitList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            var parts = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }
            return parts;
        }

        private static string[] MergeStringList(string[] existing, string[] items)
        {
            if (items == null || items.Length == 0)
            {
                return existing;
            }

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (existing != null)
            {
                foreach (var item in existing)
                {
                    var trimmed = item?.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                    {
                        result.Add(trimmed);
                    }
                }
            }

            foreach (var item in items)
            {
                var trimmed = item?.Trim();
                if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result.Count == 0 ? null : result.ToArray();
        }

        private static string ReadValue(string valuePart, out string trailing)
        {
            trailing = string.Empty;
            if (valuePart.Length == 0)
            {
                return string.Empty;
            }

            if (valuePart[0] != '`')
            {
                return valuePart.Trim();
            }

            var escaped = false;
            var sb = new StringBuilder();
            for (int i = 1; i < valuePart.Length; i++)
            {
                var c = valuePart[i];
                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '`')
                {
                    trailing = valuePart.Substring(i + 1).Trim();
                    return sb.ToString();
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static List<string> SplitTopLevel(string text, char separator)
        {
            var result = new List<string>();
            var start = 0;
            var inQuoted = false;
            var escaped = false;

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuoted)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (c == '`')
                    {
                        inQuoted = false;
                    }

                    continue;
                }

                if (c == '`')
                {
                    inQuoted = true;
                    continue;
                }

                if (c == separator)
                {
                    result.Add(text.Substring(start, i - start));
                    start = i + 1;
                }
            }

            result.Add(text.Substring(start));
            return result;
        }

        private static int IndexOfTopLevel(string text, char target)
        {
            var inQuoted = false;
            var escaped = false;

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuoted)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (c == '`')
                    {
                        inQuoted = false;
                    }

                    continue;
                }

                if (c == '`')
                {
                    inQuoted = true;
                    continue;
                }

                if (c == target)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
