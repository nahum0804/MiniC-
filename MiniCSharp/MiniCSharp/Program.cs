using Antlr4.Runtime;
using generated.parser;
using MiniCSharp.checker;
using MiniCSharp.domain.errors;
using Antlr4.Runtime.Tree;

namespace MiniCSharp;

class Program
{
    private static void Main()
    {
        {
            const string inputText = """
                                     class TestStmts {
                                         int x;
                                         bool b;
                                         string s;
                                     
                                         int main(int p) {
                                             int w;
                                             x = 42;
                                             s = "Hola";
     
                                             write(s, 10);
                                             read(x);
                                     
                                             if (x > 0) 
                                                 x = x - 1;
                                             else 
                                                 x = x + 1;
                                         }
                                         
                                         int e; 
                                     }
                                     """;

            // Arbol sintáctico
            var inputStream = new AntlrInputStream(inputText);
            var lexer = new MiniCSLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new LexerErrorListener());

            var tokens = new CommonTokenStream(lexer);

            var parser = new MiniCSParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ParserErrorListener());

            var tree = parser.program();

            Console.WriteLine("=== Árbol sintáctico ===");
            MiniCSharp.checker.DisplayTree.PrintTree(tree);
            Console.WriteLine("========================\n");

            // MiniCSChecker
            var checker = new MiniCSChecker();
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