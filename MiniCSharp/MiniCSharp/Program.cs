using Antlr4.Runtime;
using generated.parser;
using MiniCSharp.checker;
using MiniCSharp.domain.errors;

namespace MiniCSharp;

class Program
{
    private static void Main()
    {
        {
            const string inputText = """

                                                     class Prueba {
                                                         int x, y;
                                                         void foo(int a) {
                                                             int z;
                                                             z = a + x;      // x ha sido declarado globalmente
                                                             write(z, 10);
                                                         }
                                                         
                                                         int j;
                                                     }
                                                 
                                     """;

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
            Console.WriteLine(tree.ToStringTree(parser));
            Console.WriteLine("========================\n");

            var symVisitor = new SymbolTableVisitor();
            symVisitor.Visit(tree);

            var table = symVisitor.Table;
            table.Print();

            Console.WriteLine("\n--- Fin del análisis semántico (tabla de símbolos) ---");
            Console.ReadKey();
        }
    }
}