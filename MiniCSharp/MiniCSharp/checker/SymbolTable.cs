using Antlr4.Runtime;
using MiniCSharp.utils;

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

    public bool LookupField(int classTypeTag, string fieldName, out Symbol? field)
    {
        field = null;

        var className = TypeTag.ClassNameFromTag(classTypeTag);
        if (className == null || !classFields.TryGetValue(className, out var fields))
            return false;

        return fields.TryGetValue(fieldName, out field);
    }


    private readonly Dictionary<string, Dictionary<string, Symbol>> classFields = new();

    public bool InsertField(string className, IToken token, int typeTag, bool isConstant = false)
    {
        if (!classFields.ContainsKey(className))
            classFields[className] = new Dictionary<string, Symbol>();

        var fields = classFields[className];

        if (fields.ContainsKey(token.Text))
            return false;

        var fieldSymbol = new VariableSymbol(token, typeTag, CurrentLevel, null, isConstant);
        fields[token.Text] = fieldSymbol;
        return true;
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

    public bool InsertClass(IToken token, int typeTag, ParserRuleContext ctx)
    {
        var name = token.Text;
        if (LookupInCurrentLevel(name) != null) return false;

        var symbol = new VariableSymbol(token, typeTag, CurrentLevel, ctx, isConstant: false);
        _symbols.AddFirst(symbol);
        return true;
    }


    public bool IsClassDeclared(string name)
    {
        var sym = _symbols.FirstOrDefault(s => s.Token.Text == name);
        if (sym == null) return false;
        return TypeTag.IsCustomClass(sym.TypeTag);
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