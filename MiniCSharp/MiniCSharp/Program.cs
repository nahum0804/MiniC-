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
                                                         void foo(int a) { }  // redeclaración: error semántico
                                                     }
                                                 
                                     """;

            // 1) LEXER con captura de errores léxicos (si ya tienes tu CustomErrorListener, regístralo aquí)
            var inputStream = new AntlrInputStream(inputText);
            var lexer = new MiniCSLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new LexerErrorListener());

            // 2) TOKEN STREAM
            var tokens = new CommonTokenStream(lexer);

            // 3) PARSER con captura de errores sintácticos
            var parser = new MiniCSParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ParserErrorListener());

            // 4) INVOCAR la regla inicial “program”
            var tree = parser.program();

            Console.WriteLine("=== Árbol sintáctico ===");
            Console.WriteLine(tree.ToStringTree(parser));
            Console.WriteLine("========================\n");

            // 5) Construir la tabla de símbolos con nuestro SymbolTableVisitor
            var symVisitor = new SymbolTableVisitor();
            symVisitor.Visit(tree);

            // 6) Imprimir la tabla para depurar (verás variables, métodos, niveles, etc.)
            var table = symVisitor.Table;
            table.Print();

            Console.WriteLine("\n--- Fin del análisis semántico (tabla de símbolos) ---");
            Console.ReadKey();
        }
    }
}