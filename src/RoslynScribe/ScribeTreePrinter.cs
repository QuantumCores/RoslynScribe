using System;

namespace RoslynScribe
{
    internal class ScribeTreePrinter
    {
        public static void Print(ScribeNode root)
        {
            Traverse(root, 0);
        }

        private static void Traverse(ScribeNode node, int level)
        {
            var name = node.ToString();
            Console.WriteLine(new String(' ', level) + name);
            foreach (var child in node.ChildNodes)
            {
                Traverse(child, level + 1);
            }
        }
    }
}
