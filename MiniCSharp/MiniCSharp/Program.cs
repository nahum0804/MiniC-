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
                                     
                                         void main() {
                                             // 1) Asignación y uso de variables
                                             x = 42;
                                             s = "Hola";
                                             
                                             // 2) WRITE y READ (suponemos que READ siempre es válido semánticamente)
                                             write(s, 10);
                                             //read(x);  // descomentar para probar read
                                     
                                             // 3) IF / ELSE
                                             if (x > 0) 
                                                 x = x - 1;
                                             else 
                                                 x = x + 1;
                                     
                                             // 4) FOR (init; cond; post) cuerpo
                                            /*for (x = 0; x < 5; x = x + 1) {
                                                 write(x, 0);
                                                 if (x == 3) break;
                                             }
                                     
                                             // 5) WHILE
                                             while (b) {
                                                 x = x + 2;
                                                 if (x > 10) break;
                                             }
                                     
                                             // 6) SWITCH / CASE / DEFAULT
                                             switch (x) {
                                                 case 0:
                                                     s = "cero";
                                                     break;
                                                 case 1:
                                                     s = "uno";
                                                     break;
                                                 default:
                                                     s = "otro";
                                             }
                                     
                                             // 7) RETURN con y sin expresión
                                             return;
                                         }
                                     
                                         int valor() {
                                             return x * 2;
                                             */
                                         }
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