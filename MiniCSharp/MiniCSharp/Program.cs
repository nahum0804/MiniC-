﻿using System.Reflection;
using System.Reflection.Emit;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MiniCSharp.checker;
using MiniCSharp.codeGen;
using MiniCSharp.domain.errors;

namespace MiniCSharp;

class Program
{
    private static void Main()
    {
        {
           const string filePath = "Test.txt";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Archivo no encontrado: {Path.GetFullPath(filePath)}");
                return;
            }

            var inputText = File.ReadAllText(filePath);

            var inputStream = new AntlrInputStream(inputText);
            var lexer       = new MiniCSLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new LexerErrorListener());

            var tokens = new CommonTokenStream(lexer);

            var parser = new MiniCSParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ParserErrorListener());

            var tree = parser.program();

            Console.WriteLine("=== Árbol sintáctico ===");
            PrintTree(tree, parser, 0);
            Console.WriteLine("========================\n");

            var symVisitor = new SymbolTableVisitor();
            symVisitor.Visit(tree);
            var table = symVisitor.Table;
            table.Print();

            var checker = new MiniCSChecker { Table = table };
            checker.Visit(tree);

            if (checker.HasErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n--- Errores semánticos ---");
                foreach (var err in checker.Errors)
                    Console.WriteLine(err);
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n--- Análisis semántico exitoso. Sin errores. ---");
            Console.ResetColor();


            var asmName       = new AssemblyName("MiniCSharpProgram");
            var asmBuilder    = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            if (asmName.Name != null)
            {
                var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);

                var codeGen = new CodeGenVisitor(moduleBuilder, table, checker.ExprTypes);
                codeGen.Generate(tree);
            }

            var programType = asmBuilder.GetType("P");
            var mainMethod  = programType?.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
            mainMethod?.Invoke(null, null);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("¡Ejecución exitosa del código generado en memoria!");
            Console.ResetColor();
        }
    }

    private static void PrintTree(IParseTree node, Parser parser, int indent)
    {
        var pad = new string(' ', indent * 2);

        switch (node)
        {
            case ParserRuleContext ctx:
            {
                var ruleName = parser.RuleNames[ctx.RuleIndex];
                Console.WriteLine($"{pad}{ruleName}");
                break;
            }
            case ITerminalNode t:
                Console.WriteLine($"{pad}{t.Symbol.Type}:'{t.GetText()}'");
                break;
            default:
                Console.WriteLine($"{pad}{node.GetType().Name}: '{node.GetText()}'");
                break;
        }

        for (var i = 0; i < node.ChildCount; i++)
            PrintTree(node.GetChild(i), parser, indent + 1);
    }
}