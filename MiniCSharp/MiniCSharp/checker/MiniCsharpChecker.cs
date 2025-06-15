using Antlr4.Runtime;
using MiniCSharp.utils;

namespace MiniCSharp.checker
{
    public class MiniCSChecker : MiniCSParserBaseVisitor<object>
    {
        public SymbolTable Table { get; set; } = new();
        private readonly List<string> _errors = new();

        public IEnumerable<string> Errors => _errors;
        public bool HasErrors => _errors.Count > 0;

        private int _loopDepth = 0;

        private void Report(string msg, IToken tok)
        {
            _errors.Add($"Error semántico: {msg} (línea {tok.Line}, col {tok.Column})");
        }

        public override object VisitProgram(MiniCSParser.ProgramContext ctx)
        {
            foreach (var c in ctx.classDecl())
                VisitClassDecl(c);
            foreach (var v in ctx.varDecl())
                VisitVarDecl(v);
            foreach (var m in ctx.methodDecl())
                VisitMethodDecl(m);
            return null;
        }

        public override object VisitClassDecl(MiniCSParser.ClassDeclContext ctx)
        {
            return null;
        }

        public override object VisitVarDecl(MiniCSParser.VarDeclContext ctx)
        {
            int tag = TypeTag.FromTypeNameWithBrackets(ctx.type().GetText());
            foreach (var id in ctx.ident())
            {
                if (Table.Lookup(id.GetText()) == null)
                    Report("Variable no declarada", id.Start);
            }
            return null;
        }

        public override object VisitDesignator(MiniCSParser.DesignatorContext ctx)
        {
            if (ctx.ident().Length == 1)
            {
                var name = ctx.ident(0).GetText();
                if (Table.Lookup(name) == null)
                    Report("Símbolo no declarado", ctx.ident(0).Start);
            }
            else if (ctx.ident().Length == 2)
            {
                var objName = ctx.ident(0).GetText();
                var fieldName = ctx.ident(1).GetText();
                var objSymbol = Table.Lookup(objName);

                if (objSymbol == null)
                {
                    Report("Objeto no declarado", ctx.ident(0).Start);
                }
                else if (!Table.LookupField(objSymbol.TypeTag, fieldName, out _))
                {
                    Report($"Campo '{fieldName}' no encontrado en el tipo del objeto '{objName}'", ctx.ident(1).Start);
                }
            }

            return null;
        }


        public override object VisitAssignStmt(MiniCSParser.AssignStmtContext context)
        {
            Visit(context.designator());
            _ = Visit(context.expr());
            return null;
        }

        public override object VisitReadStmt(MiniCSParser.ReadStmtContext context)
        {
            Visit(context.designator());
            return null;
        }

        public override object VisitWriteStmt(MiniCSParser.WriteStmtContext context)
        {
            Visit(context.expr());
            return null;
        }

        public override object VisitBlock(MiniCSParser.BlockContext ctx)
        {
            foreach (var vd in ctx.varDecl()) VisitVarDecl(vd);
            foreach (var st in ctx.statement()) Visit(st);
            return null;
        }

        public override object VisitMethodDecl(MiniCSParser.MethodDeclContext ctx)
        {
            Visit(ctx.block());
            return null;
        }
    }
}