using Antlr4.Runtime;
using generated.parser;
using MiniCSharp.checker;
using MiniCSharp.domain.errors;
using Antlr4.Runtime.Tree;
using MiniCSharp.checker.semanticChecker;
using MiniCSharp.checker.symbolTable;

namespace MiniCSharp;

internal static class Program
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
            var lexer = new MiniCSLexer(inputStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new MiniCSParser(tokens);
            var tree = parser.program();

            // Generar tabla de símbolos
            var symbolTableBuilder = new SymbolTableVisitor();
            symbolTableBuilder.Visit(tree);
            Console.WriteLine("----- SÍMBOLOS ACTIVOS -----");
            symbolTableBuilder.Table.PrintActive();
            // Usar la tabla en el checker
            var checker = new MiniCsChecker(symbolTableBuilder.Table);
            checker.Visit(tree);

            //Tabla de simbolos
            //Console.WriteLine("=== TABLA DE SÍMBOLOS ACTIVOS ===");
            //checker.Table.PrintActive();

            checker.Table.Print();

            if (checker.HasErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("=== ERRORES SEMÁNTICOS ===");
                foreach (var err in checker.Errors)
                    Console.WriteLine("  " + err);
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Compiled Successfully - Happy Coding :)");
                Console.ResetColor();
            }

            Console.WriteLine("\n--- Fin del análisis semántico ---");
            Console.ReadKey();
        }
    }
}