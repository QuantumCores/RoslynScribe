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
            var labeled = new HashSet<Guid>();
            foreach (var node in scribeResult.Trees)
            {
                Traverse(sb, node, scribeResult.Nodes, transitions, labeled);
                sb.AppendLine("");
                sb.AppendLine("");
            }

            return sb.ToString();
        }

        private static void Traverse(
            StringBuilder sb,
            ScribeTreeNode node,
            Dictionary<Guid, ScribeNodeData> nodes,
            HashSet<(Guid, Guid)> transitions,
            HashSet<Guid> labeled)
        {
            var current = nodes[node.Id];
            var showCurrentValue = labeled.Add(node.Id);

            if (node.ChildNodes.Count != 0)
            {
                foreach (var child in node.ChildNodes)
                {
                    var tmp = nodes[child.Id];
                    var showTmpValue = labeled.Add(child.Id);

                    var transition = (current.Id, tmp.Id);
                    if(!transitions.Contains(transition))
                    {
                        transitions.Add(transition);
                        sb.AppendLine($"{GetText(current, showCurrentValue)} --- {GetText(tmp, showTmpValue)}");
                    }

                    Traverse(sb, child, nodes, transitions, labeled);
                }
            }
        }

        private static string GetText(ScribeNodeData node, bool showValue)
        {
            var result = node.Kind == "Document" ? node.MetaInfo.DocumentName : node.Id.ToString();
            
            if (node.Guides != null && showValue)
            {
                var shape = GetShape(node);
                result += shape[0] + "\"'" + string.Join(", ", node.Guides.Text) + "'\"" + shape[1];
            }

            return result;
        }

        private static string[] GetShape(ScribeNodeData node)
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
