using Antlr4.Runtime;
using generated.parser;

namespace MiniCSharp.checker.symbolTable;

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
            _symbols.AddLast(sym);
            _allSymbols.Add(sym);   
            return true;
        }

        public bool InsertMethod(IToken token, int returnTag, List<int> paramTypeTags, ParserRuleContext declCtx)
        {
            var name = token.Text;
            if (LookupInCurrentLevel(name) != null) return false;

            var sym = new MethodSymbol(token, returnTag, CurrentLevel, declCtx, paramTypeTags);
            _symbols.AddLast(sym);
            _allSymbols.Add(sym);    
            return true;
        }
        
        public bool InsertClass(IToken token, ParserRuleContext declCtx)
        {
            var name = token.Text;
            if (LookupInCurrentLevel(name) != null) return false;

            var sym = new ClassSymbol(token, TypeTags.Class, CurrentLevel, declCtx);
            _symbols.AddLast(sym);
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
            var typeName = TypeTags.Name(sym.TypeTag);

            Console.Write($"Name: {sym.Token.Text}, Level: {sym.ScopeLevel}, Type: {typeName}");
            
            switch (sym)
            {
                case MethodSymbol ms when ms.DeclContext is MiniCSParser.MethodDeclContext mdCtx:
                {
                    var form = mdCtx.formPars();
                    var paramTypes = form != null
                        ? form.type().Select(t => TypeTags.Name(TypeTags.FromTypeName(t.GetText())))
                        : Enumerable.Empty<string>();
                    Console.Write($", Params=[{string.Join(",", paramTypes)}]");
                    break;
                }
                case VariableSymbol vv:
                    Console.Write($", Var(isConst={vv.IsConstant})");
                    break;
                case ClassSymbol:
                    Console.Write(", Class");
                    break;
            }

            Console.WriteLine();
        }
        
        
}