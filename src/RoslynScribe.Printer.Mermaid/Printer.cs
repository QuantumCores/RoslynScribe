using RoslynScribe.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoslynScribe.Printer.Mermaid
{
    public class Printer
    {
        public static async Task Print(PrintingOptions options)
        {
            var result = await RoslynScribe.Program.Run();
            var flow = Print(result, options);
        }

        public static async Task Print(PrintingOptions options, string filePath)
        {
            var result = JsonSerializer.Deserialize<ScribeResult>(filePath);
            var flow = Print(result, options);
        }

        internal static string Print(ScribeResult scribeResult, PrintingOptions options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine(options.Title);
            sb.AppendLine("---");
            sb.AppendLine("flowchart LR");

            // transitions set to avoid duplicated connections between nodes
            var transitions = new HashSet<(Guid, Guid)>();
            foreach (var node in scribeResult.Trees)
            {
                Traverse(sb, node, scribeResult.Nodes, transitions);
                sb.AppendLine("");
                sb.AppendLine("");
            }

            return sb.ToString();
        }

        private static void Traverse(StringBuilder sb, ScribeNode node, Dictionary<Guid, ScribeNode> nodes, HashSet<(Guid, Guid)> transitions)
        {
            var current = node;
            var isCurrentUnique = true;
            if (node.TargetNodeId != null)
            {
                current = nodes[node.TargetNodeId.Value];
                isCurrentUnique = false;
            }

            // what if there are no child nodes? And node is referenced from another project?
            if (current.ChildNodes.Count != 0)
            {
                foreach (var child in current.ChildNodes)
                {
                    var tmp = child;
                    var isTmpUnique = true;
                    if (child.TargetNodeId != null)
                    {
                        tmp = nodes[child.TargetNodeId.Value];
                        isTmpUnique = false;
                    }

                    var transition = (current.Id, tmp.Id);
                    if(!transitions.Contains(transition))
                    {
                        transitions.Add(transition);
                        sb.AppendLine($"{GetText(current, isCurrentUnique)} --- {GetText(tmp, isTmpUnique)}");
                    }

                    Traverse(sb, tmp, nodes, transitions);
                }
            }
        }

        private static string GetText(ScribeNode node, bool isUnique)
        {
            var result = node.Kind == "Document" ? node.MetaInfo.DocumentName : node.Id.ToString();
            
            if (node.Value != null && isUnique)
            {
                var shape = GetShape(node);
                result += shape[0] + "\"'" + string.Join(", ", node.Value) + "'\"" + shape[1];
            }

            return result;
        }

        private static string[] GetShape(ScribeNode node)
        {
            switch (node.Kind)
            {
                case "IfStatement":
                    return new string[] { "{", "}" };
                default:
                    return new string[] { "(", ")" };
            }
        }
    }
}
