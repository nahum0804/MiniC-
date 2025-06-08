using System;
using Antlr4.Runtime.Tree;

namespace MiniCSharp.checker
{
    public static class DisplayTree
    {
        public static void PrintTree(IParseTree node, int level = 0)
        {
            if (node == null) return;
            
            var indent = new string(' ', level * 2);
            
            if (node is TerminalNodeImpl term)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(indent + "- ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(term.Symbol.Text);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(indent + "- ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(node.GetType().Name.Replace("Context", ""));
            }
            Console.ResetColor();
            
            for (int i = 0; i < node.ChildCount; i++)
            {
                PrintTree(node.GetChild(i), level + 1);
            }
        }
    }
}