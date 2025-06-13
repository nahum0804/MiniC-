using Antlr4.Runtime;

namespace MiniCSharp.checker.symbolTable
{
    public class ClassSymbol(
        IToken token,
        int typeTag,
        int scopeLevel,
        ParserRuleContext declContext
    ) : Symbol(token, typeTag, scopeLevel, declContext)
    {
        // Podés agregar más propiedades si querés guardar atributos como campos o métodos
    }
}