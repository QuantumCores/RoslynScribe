using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace RoslynScribe.Domain.ScribeConsole
{
    public class SyntaxTreePrinter
    {
        public static void Print(SyntaxNode root)
        {
            Traverse(root, 0);
        }

        private static void Traverse(SyntaxNode node, int level)
        {
            var descendants = node.ChildNodes();
            var name = node.Kind().ToString(); //+ " - " + node.ToString();
            Console.WriteLine(new string(' ', level) + name);
            foreach (var child in descendants)
            {
                Traverse(child, level + 1);
            }
        }
    }
}
