using Antlr4.Runtime;

namespace MiniCSharp.checker.symbolTable;

public class VariableSymbol(IToken token, int typeTag, int scopeLevel, ParserRuleContext declContext, bool isConstant)
    : Symbol(token, typeTag, scopeLevel, declContext)
{
    public bool IsConstant { get; } = isConstant;
}