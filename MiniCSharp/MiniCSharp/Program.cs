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
                                         List<int> nums;
                                         List<string> names;
                                         
                                         int x;
                                         bool b;
                                         string s;
                                     
                                         int main(int p) {
                                             nums = <1,2,3>;
                                             names = <"Hola mundo", "Hola mundo2">;
                                             List<bool> bools;
                                             List<char> chars;
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
                                         
                                         bool analysis(string u) {
                                            bools = <true,false,false>;
                                            chars = <'A','B','C'>;
                                            int i; 
                                            string l;
                                            b = true;
                                            for (i = 0; i < 10; i = i+1) {
                                                char k;
                                                l =  "For";
                                                k = 'F';
                                                write(i, 10);
                                            }
                                            
                                            bool flag;
                                            if (1 != 2) {
                                                i = 1;
                                            }
                                            
                                            while(1 < 10) {
                                              i = 10; 
                                              }
                                              
                                            return flag;
                                         }
                                         
                                         void method(){
                                            write(1, 10);
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

            //Console.WriteLine("=== Árbol sintáctico ===");
            //DisplayTree.PrintTree(tree);
            //Console.WriteLine("========================\n");

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