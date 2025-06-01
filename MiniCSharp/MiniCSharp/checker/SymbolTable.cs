using Antlr4.Runtime;

namespace MiniCSharp.checker;

public class SymbolTable
{
    private readonly LinkedList<Symbol> _symbols = [];
    private int CurrentLevel { get; set; } = -1;

    public SymbolTable()
    {
        _symbols.Clear();
        CurrentLevel = -1;
    }

    public void OpenScope()
    {
        CurrentLevel++;
    }

    public void CloseScope()
    {
        var filteredSymbol = new LinkedList<Symbol>();
        foreach (var symbol in _symbols.Where(symbol => symbol.ScopeLevel != CurrentLevel))
        {
            filteredSymbol.AddLast(symbol);
        }

        _symbols.Clear();

        foreach (var symbol in filteredSymbol)
        {
            _symbols.AddLast(symbol);
        }

        CurrentLevel--;
    }


    public Symbol? LookupInCurrentLevel(string name)
    {
        return _symbols.TakeWhile(symbol => symbol.ScopeLevel == CurrentLevel)
            .FirstOrDefault(symbol => symbol.Token.Text.Equals(name, StringComparison.Ordinal));
    }

    public Symbol? Lookup(string name)
    {
        return _symbols.FirstOrDefault(symbol => symbol.Token.Text.Equals(name, StringComparison.Ordinal));
    }

    public bool InsertVariable(IToken token, int typeTag, bool isConstant, ParserRuleContext declCtx)
    {
        var name = token.Text;

        if (LookupInCurrentLevel(name) != null) return false;

        var sym = new VariableSymbol(token, typeTag, CurrentLevel, declCtx, isConstant);
        _symbols.AddFirst(sym);
        return true;
    }

    public bool InsertMethod(IToken token, int returnTag, List<int> paramTypeTags, ParserRuleContext declCtx)
    {
        var name = token.Text;
        if (LookupInCurrentLevel(name) != null) return false;
        var sym = new MethodSymbol(token, returnTag, CurrentLevel, declCtx, paramTypeTags);
        _symbols.AddFirst(sym);
        return true;
    }

    public void Print()
    {
        Console.WriteLine($"----- TABLA DE SIMBOLOS (nivel actual = {CurrentLevel}) ------");
        foreach (var sym in _symbols)
        {
            Console.Write($"Name: {sym.Token.Text}, Level: {sym.ScopeLevel}, TypeTag: {sym.TypeTag}");
            switch (sym)
            {
                case VariableSymbol v:
                    Console.Write($", Var(isConst={v.IsConstant})");
                    break;
                case MethodSymbol m:
                    Console.Write($", Method(params=[{string.Join(",", m.ParamTypeTags)}], ret={m.ReturnTypeTag})");
                    break;
            }

            Console.WriteLine();
        }

        Console.WriteLine("------ FIN TABLA ------\n");
    }
}