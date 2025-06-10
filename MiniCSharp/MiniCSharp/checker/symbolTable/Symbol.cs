using Antlr4.Runtime;

namespace MiniCSharp.checker;

public abstract class Symbol(IToken token, int typeTag, int scopeLevel, ParserRuleContext declContext)
{
    public IToken Token { get; } = token;

    public int TypeTag { get; } = typeTag;

    public int ScopeLevel { get; } = scopeLevel;

    public ParserRuleContext DeclContext { get; } = declContext;
}