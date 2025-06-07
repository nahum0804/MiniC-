using Antlr4.Runtime;

namespace MiniCSharp.checker;

public class SymbolTable
{
    private readonly LinkedList<Symbol> _symbols = [];
    
    private readonly List<Symbol> _allSymbols = new();
    
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
            var survivors = new LinkedList<Symbol>();
            foreach (var sym in _symbols)
                if (sym.ScopeLevel != CurrentLevel)
                    survivors.AddLast(sym);
            _symbols.Clear();
            foreach (var sym in survivors)
                _symbols.AddLast(sym);

            CurrentLevel--;
        }

        public Symbol? LookupInCurrentLevel(string name)
            => _symbols
              .Where(s => s.ScopeLevel == CurrentLevel)
              .FirstOrDefault(s => s.Token.Text == name);

        public Symbol? Lookup(string name)
            => _symbols.FirstOrDefault(s => s.Token.Text == name);

        public bool InsertVariable(IToken token, int typeTag, bool isConstant, ParserRuleContext declCtx)
        {
            var name = token.Text;
            if (LookupInCurrentLevel(name) != null) return false;

            var sym = new VariableSymbol(token, typeTag, CurrentLevel, declCtx, isConstant);
            _symbols.AddFirst(sym);
            _allSymbols.Add(sym);   
            return true;
        }

        public bool InsertMethod(IToken token, int returnTag, List<int> paramTypeTags, ParserRuleContext declCtx)
        {
            var name = token.Text;
            if (LookupInCurrentLevel(name) != null) return false;

            var sym = new MethodSymbol(token, returnTag, CurrentLevel, declCtx, paramTypeTags);
            _symbols.AddFirst(sym);
            _allSymbols.Add(sym);    
            return true;
        }
        
        public void PrintActive()
        {
            Console.WriteLine($"----- SÍMBOLOS ACTIVOS (nivel actual = {CurrentLevel}) ------");
            foreach (var sym in _symbols)
                DumpSym(sym);
            Console.WriteLine("------ FIN ACTIVOS ------\n");
        }
        
        public void Print()
        {
            Console.WriteLine("===== HISTORIAL COMPLETO DE SÍMBOLOS =====");
            foreach (var sym in _allSymbols)
                DumpSym(sym);
            Console.WriteLine("===== FIN HISTORIAL =====\n");
        }

        private void DumpSym(Symbol sym)
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
        
}