using Antlr4.Runtime;

namespace MiniCSharp.checker.symbolTable;

public class MethodSymbol(
    IToken token,
    int returnTypeTag,
    int scopeLevel,
    ParserRuleContext declContext,
    List<int> paramTypeTags) : Symbol(token, returnTypeTag, scopeLevel, declContext)
{
    public int ReturnTypeTag { get; } = returnTypeTag;
    public List<int> ParamTypeTags { get; } = paramTypeTags;
}