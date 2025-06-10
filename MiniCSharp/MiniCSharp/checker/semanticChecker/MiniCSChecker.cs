using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using generated.parser;

namespace MiniCSharp.checker
{
    public class MiniCSChecker : MiniCSParserBaseVisitor<object>
    {
        public SymbolTable Table { get; } = new SymbolTable();
        private readonly List<string> _errors = new();

        public IEnumerable<string> Errors   => _errors;
        public bool           HasErrors => _errors.Count > 0;
        
        private int _loopDepth = 0;

        private void Report(string msg, IToken tok)
        {
            _errors.Add($"Error semántico: {msg} '{tok.Text}' (línea {tok.Line}, col {tok.Column})");
        }

        public override object VisitProgram(MiniCSParser.ProgramContext ctx)
        {
            Table.OpenScope();
            
            var classTok = ctx.ident().Start;
            if (!Table.InsertVariable(classTok, TypeTags.Class, isConstant: true, ctx))
                Report("Clase redeclarada", classTok);
            
            Table.OpenScope();
            foreach (var v in ctx.varDecl())    VisitVarDecl(v);
            foreach (var m in ctx.methodDecl()) VisitMethodDecl(m);
            foreach (var c in ctx.classDecl())  VisitClassDecl(c);
            Table.CloseScope();

            Table.CloseScope();
            return null;
        }

        public override object VisitVarDecl(MiniCSParser.VarDeclContext ctx)
        {
            int tag = TypeTags.FromTypeName(ctx.type().GetText());
            foreach (var id in ctx.ident())
            {
                var tok = id.Start;
                if (!Table.InsertVariable(tok, tag, isConstant: false, ctx))
                    Report("Variable redeclarada", tok);
            }
            return null;
        }

        public override object VisitClassDecl(MiniCSParser.ClassDeclContext ctx)
        {
            var classTok = ctx.ident().Start;
            if (!Table.InsertVariable(classTok, TypeTags.Class, isConstant: true, ctx))
                Report("Clase redeclarada", classTok);

            Table.OpenScope();
            foreach (var v in ctx.varDecl())    VisitVarDecl(v);
            foreach (var m in ctx.methodDecl()) VisitMethodDecl(m);
            Table.CloseScope();
            return null;
        }

        public override object VisitMethodDecl(MiniCSParser.MethodDeclContext ctx)
        {
            int retTag = ctx.VOID() != null
                ? TypeTags.Void
                : TypeTags.FromTypeName(ctx.type().GetText());

            
            var paramTags = new List<int>();
            if (ctx.formPars() != null)
                foreach (var t in ctx.formPars().type())
                    paramTags.Add(TypeTags.FromTypeName(t.GetText()));

            var mTok = ctx.ident().Start;
            if (!Table.InsertMethod(mTok, retTag, paramTags, ctx))
                Report("Método redeclarado", mTok);

            
            Table.OpenScope();
            if (ctx.formPars() != null)
            {
                var fp = ctx.formPars();
                for (int i = 0; i < fp.ident().Length; i++)
                {
                    var pTok = fp.ident(i).Start;
                    var pTag = TypeTags.FromTypeName(fp.type(i).GetText());
                    if (!Table.InsertVariable(pTok, pTag, isConstant: false, fp))
                        Report("Parámetro redeclarado", pTok);
                }
            }

            Visit(ctx.block());
            Table.CloseScope();
            return null;
        }

        public override object VisitBlock(MiniCSParser.BlockContext ctx)
        {
            Table.OpenScope();
            foreach (var v in ctx.varDecl())    VisitVarDecl(v);
            foreach (var s in ctx.statement())  Visit(s);
            Table.CloseScope();
            return null;
        }

        public override object VisitDesignator(MiniCSParser.DesignatorContext ctx)
        {
            var name = ctx.ident(0).GetText();
            if (Table.Lookup(name) == null)
                Report("Símbolo no declarado", ctx.ident(0).Start);
            return null;
        }

        public override object VisitAssignStatement(MiniCSParser.AssignStatementContext ctx)
        {
            var sym = Table.Lookup(ctx.designator().GetText());
            if (sym is not VariableSymbol varSym)
            {
                Report("No es una variable", ctx.designator().Start);
            }
            else
            {
                int lhsTag = varSym.TypeTag;
                int rhsTag = (int)VisitExpr(ctx.expr());

                if (lhsTag != rhsTag)
                {
                    Report(
                        $"Incompatibilidad de tipos en asignación (se esperaba {TypeTags.Name(lhsTag)}, vino {TypeTags.Name(rhsTag)})",
                        ctx.ASSIGN().Symbol
                    );
                }
            }
            return null;
        }

        public override object VisitIfStatement(MiniCSParser.IfStatementContext ctx)
        {
            int condTag = (int)Visit(ctx.condition());
            if (condTag != TypeTags.Bool)
                Report("Condición de IF no booleana", ctx.IF().Symbol);

            Visit(ctx.statement(0));
            if (ctx.ELSE() != null) Visit(ctx.statement(1));
            return null;
        }

        public override object VisitForStatement(MiniCSParser.ForStatementContext ctx)
        {
            _loopDepth++;
            Visit(ctx.forInit());
            if (ctx.condition() != null)
            {
                int condTag = (int)Visit(ctx.condition());
                if (condTag != TypeTags.Bool)
                    Report("Condición de FOR no booleana", ctx.FOR().Symbol);
            }
            Visit(ctx.forUpdate());
            Visit(ctx.statement());
            _loopDepth--;
            return null;
        }

        public override object VisitWhileStatement(MiniCSParser.WhileStatementContext ctx)
        {
            _loopDepth++;
            int condTag = (int)Visit(ctx.condition());
            if (condTag != TypeTags.Bool)
                Report("Condición de WHILE no booleana", ctx.WHILE().Symbol);
            Visit(ctx.statement());
            _loopDepth--;
            return null;
        }

        public override object VisitBreakStatement(MiniCSParser.BreakStatementContext ctx)
        {
            if (_loopDepth == 0)
                Report("Break fuera de bucle", ctx.BREAK().Symbol);
            return null;
        }

        public override object VisitReturnStatement(MiniCSParser.ReturnStatementContext ctx)
        {
            if (ctx.expr() != null)
                _ = VisitExpr(ctx.expr());
            return null;
        }

        public override object VisitReadStatement(MiniCSParser.ReadStatementContext ctx) => null;

        public override object VisitWriteStatement(MiniCSParser.WriteStatementContext ctx)
        {
            _ = VisitExpr(ctx.expr());
            return null;
        }

        public override object VisitSwitchStatement(MiniCSParser.SwitchStatementContext ctx)
        {
            _ = VisitExpr(ctx.expr());
            foreach (var cb in ctx.caseBlock()) Visit(cb);
            foreach (var s in ctx.statement())  Visit(s);
            return null;
        }

        public override object VisitBlackStatement(MiniCSParser.BlackStatementContext ctx)
        {
            return VisitBlock(ctx.block());
        }

        public override object VisitEmptyStatement(MiniCSParser.EmptyStatementContext ctx) => null;

        public override object VisitExpr(MiniCSParser.ExprContext ctx)
        {
            int type = (int)VisitTerm(ctx.term(0));
            for (int i = 1; i < ctx.term().Length; i++)
            {
                var opNode = ctx.addop(i - 1);
                string op    = opNode.GetText();
                int rhs      = (int)VisitTerm(ctx.term(i));
                type = TypeChecker.Compatible(type, rhs, op);
                if (type == TypeTags.Unknown)
                    Report($"Operador '{op}' no válido entre tipos", opNode.Start);
            }
            return type;
        }

        public override object VisitTerm(MiniCSParser.TermContext ctx)
        {
            int type = (int)VisitFactor(ctx.factor(0));
            for (int i = 1; i < ctx.factor().Length; i++)
            {
                var opNode = ctx.mulop(i - 1);
                string op    = opNode.GetText();
                int rhs      = (int)VisitFactor(ctx.factor(i));
                type = TypeChecker.Compatible(type, rhs, op);
                if (type == TypeTags.Unknown)
                    Report($"Operador '{op}' no válido entre tipos", opNode.Start);
            }
            return type;
        }

        public override object VisitFactor(MiniCSParser.FactorContext ctx)
        {
            if (ctx.listLiteral() != null)
                return VisitListLiteral(ctx.listLiteral());
            if (ctx.NUMLIT() != null)    return TypeTags.Int;
            if (ctx.CHARLIT() != null)   return TypeTags.Char;
            if (ctx.STRINGLIT() != null) return TypeTags.String;
            if (ctx.TRUE()  != null ||
                ctx.FALSE() != null)     return TypeTags.Bool;

            if (ctx.designator() != null)
            {
                var sym = Table.Lookup(ctx.designator().GetText());
                return sym?.TypeTag
                       ?? ReportAndReturn(TypeTags.Unknown, ctx.designator().Start);
            }

            // ( expr )
            if (ctx.LEFTP() != null)
                return VisitExpr(ctx.expr());

            return TypeTags.Unknown;
        }

        public override object VisitCondFact(MiniCSParser.CondFactContext ctx)
        {
            int left  = (int)Visit(ctx.expr(0));
            int right = (int)Visit(ctx.expr(1));
            string op = ctx.relop().GetText();

            int result = TypeChecker.Compatible(left, right, op);
            if (result != TypeTags.Bool)
                Report($"Operador relacional '{op}' no válido entre tipos", ctx.relop().Start);

            return TypeTags.Bool;
        }

        public override object VisitCondTerm(MiniCSParser.CondTermContext ctx)
        {
            _ = Visit(ctx.condFact(0));
            for (int i = 1; i < ctx.condFact().Length; i++)
            {
                int t2 = (int)Visit(ctx.condFact(i));
                if (t2 != TypeTags.Bool)
                    Report("Operador '&&' aplicado a no-bool", ctx.AND(i - 1).Symbol);
            }
            return TypeTags.Bool;
        }

        public override object VisitCondition(MiniCSParser.ConditionContext ctx)
        {
            _ = Visit(ctx.condTerm(0));
            for (int i = 1; i < ctx.condTerm().Length; i++)
            {
                int t2 = (int)Visit(ctx.condTerm(i));
                if (t2 != TypeTags.Bool)
                    Report("Operador '||' aplicado a no-bool", ctx.OR(i - 1).Symbol);
            }
            return TypeTags.Bool;
        }

        public override object VisitListLiteral(MiniCSParser.ListLiteralContext ctx)
        {
            // < expr1, expr2, ... >
            int elemType = (int)VisitExpr(ctx.expr(0));
            for (int i = 1; i < ctx.expr().Length; i++)
            {
                int t = (int)VisitExpr(ctx.expr(i));
                if (t != elemType)
                    Report("Elementos de lista con tipos distintos", ctx.expr(i).Start);
            }
            return TypeTags.ListOf(elemType);
        }

        private int ReportAndReturn(int tag, IToken tok)
        {
            Report("Símbolo no declarado", tok);
            return tag;
        }
    }
}
