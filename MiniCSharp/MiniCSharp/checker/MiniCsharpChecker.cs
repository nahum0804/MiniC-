using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
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

        private int _currentReturnTag;


        public Dictionary<ParserRuleContext, int> ExprTypes { get; } = new();

        private void Report(string msg, IToken tok)
        {
            _errors.Add($"Error semántico: {msg} (línea {tok.Line}, col {tok.Column})");
        }

        public override object VisitClassDecl(MiniCSParser.ClassDeclContext ctx)
        {
            return null;
        }

        public override object VisitCallStmt(MiniCSParser.CallStmtContext ctx)
        {
            var name = ctx.designator().GetText();
            var sym = Table.Lookup(name) as MethodSymbol;
            if (sym == null)
            {
                Report($"Método '{name}' no declarado", ctx.Start);
                return TypeTag.Void;
            }

            var args = ctx.actPars()?.expr() ?? Array.Empty<MiniCSParser.ExprContext>();
            if (args.Length != sym.ParamTypeTags.Count)
            {
                Report(
                    $"'{name}' espera {sym.ParamTypeTags.Count} args, recibe {args.Length}",
                    ctx.Start
                );
            }

            for (int i = 0; i < args.Length && i < sym.ParamTypeTags.Count; i++)
            {
                int actual = (int)Visit(args[i]);
                int esperado = sym.ParamTypeTags[i];
                if (actual != esperado)
                {
                    Report(
                        $"En llamada a '{name}', el parámetro {i + 1} es {TypeTag.PrettyPrint(actual)} " +
                        $"pero se esperaba {TypeTag.PrettyPrint(esperado)}",
                        args[i].Start
                    );
                }
            }

            return TypeTag.Void;
        }


        private int PromoteNumeric(int a, int b)
        {
            if (a == TypeTag.Double || b == TypeTag.Double)
                return TypeTag.Double;
            if (a == TypeTag.Float || b == TypeTag.Float)
                return TypeTag.Float;
            if (a == TypeTag.Int && b == TypeTag.Int)
                return TypeTag.Int;
            throw new InvalidOperationException($"Tipos no compatibles en operación numérica: {a}, {b}");
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

        public override object VisitExpr(MiniCSParser.ExprContext ctx)
        {
            int t0;

            bool isUnaryNeg = ctx.BAR() != null;
            if (isUnaryNeg)
            {
                t0 = (int)Visit(ctx.term(0));
                if (t0 != TypeTag.Int && t0 != TypeTag.Float && t0 != TypeTag.Double)
                    Report($"El operador unario '-' sólo aplica a numéricos, no a {TypeTag.PrettyPrint(t0)}",
                        ctx.Start);
            }
            else if (ctx.cast() != null)
            {
                t0 = (int)VisitCast(ctx.cast());
                int actual = (int)Visit(ctx.term(0));
                if (actual != t0)
                    Report($"No se puede castear de {TypeTag.PrettyPrint(actual)} a {TypeTag.PrettyPrint(t0)}",
                        ctx.Start);
            }
            else
            {
                t0 = (int)Visit(ctx.term(0));
            }


            for (int i = 1; i < ctx.term().Length; i++)
            {
                int ti = (int)Visit(ctx.term(i));

                if ((t0 != TypeTag.Int && t0 != TypeTag.Float && t0 != TypeTag.Double) ||
                    (ti != TypeTag.Int && ti != TypeTag.Float && ti != TypeTag.Double))
                {
                    Report("Los operadores '+' y '-' sólo aplican a tipos numéricos",
                        ctx.Start);
                }

                t0 = PromoteNumeric(t0, ti);
            }

            ExprTypes[ctx] = t0;
            return t0;
        }


        public override object VisitTerm(MiniCSParser.TermContext context)
        {
            var t0 = (int)Visit(context.factor(0));
            for (var i = 1; i < context.factor().Length; i++)
            {
                var ti = (int)Visit(context.factor(i));
                t0 = PromoteNumeric(t0, ti);
            }

            ExprTypes[context] = t0;
            return t0;
        }

        public override object VisitFactor(MiniCSParser.FactorContext ctx)
        {
            if (ctx.NEW() != null && ctx.SBL().Length == 0 && ctx.LEFTP() != null)
            {
                var className = ctx.ident().GetText();
                if (!Table.IsClassDeclared(className))
                    Report($"Clase '{className}' no declarada", ctx.ident().Start);
                int tag = TypeTag.RegisterClass(className);
                ExprTypes[ctx] = tag;
                return tag;
            }

            if (ctx.NEW() != null && ctx.SBL().Length > 0)
            {
                var baseName = ctx.ident().GetText();
                int baseTag = TypeTag.FromTypeName(baseName);
                if (baseTag == TypeTag.Unknown)
                    Report($"Tipo base desconocido: {baseName}", ctx.ident().Start);

                foreach (var idx in ctx.expr())
                {
                    int idxTag = (int)Visit(idx);
                    if (idxTag != TypeTag.Int)
                        Report("Índice de arreglo debe ser int", idx.Start);
                }

                int dims = ctx.SBL().Length;
                int arrayTag = baseTag + dims * TypeTag.ListBase;
                ExprTypes[ctx] = arrayTag;
                return arrayTag;
            }

            if (ctx.designator() != null)
            {
                return VisitDesignatorAndGetType(ctx.designator());
            }

            // 4) literales
            if (ctx.NUMLIT() != null)
            {
                ExprTypes[ctx] = TypeTag.Int;
                return TypeTag.Int;
            }

            if (ctx.FLOATLIT() != null)
            {
                ExprTypes[ctx] = TypeTag.Float;
                return TypeTag.Float;
            }

            if (ctx.DOUBLELIT() != null)
            {
                ExprTypes[ctx] = TypeTag.Double;
                return TypeTag.Double;
            }

            if (ctx.CHARLIT() != null)
            {
                ExprTypes[ctx] = TypeTag.Char;
                return TypeTag.Char;
            }

            if (ctx.STRINGLIT() != null)
            {
                ExprTypes[ctx] = TypeTag.String;
                return TypeTag.String;
            }

            if (ctx.TRUE() != null || ctx.FALSE() != null)
            {
                ExprTypes[ctx] = TypeTag.Bool;
                return TypeTag.Bool;
            }

            // 5) paréntesis
            if (ctx.LEFTP() != null && ctx.expr().Length > 0)
            {
                var t = (int)Visit(ctx.expr(0));
                ExprTypes[ctx] = t;
                return t;
            }

            Report($"Factor no soportado: {ctx.GetText()}", ctx.Start);
            ExprTypes[ctx] = TypeTag.Unknown;
            return TypeTag.Unknown;
        }


        public override object VisitIncStmt(MiniCSParser.IncStmtContext ctx)
        {
            int tag = (int)VisitDesignator(ctx.designator());
            if (tag != TypeTag.Int)
                Report("++ sólo aplica a variables int", ctx.designator().Start);
            return TypeTag.Void;
        }

        public override object VisitDecStmt(MiniCSParser.DecStmtContext ctx)
        {
            int tag = (int)VisitDesignator(ctx.designator());
            if (tag != TypeTag.Int)
                Report("-- sólo aplica a variables int", ctx.designator().Start);
            return TypeTag.Void;
        }


        public override object VisitCondFact(MiniCSParser.CondFactContext ctx)
        {
            var left = (int)Visit(ctx.expr(0));
            var right = (int)Visit(ctx.expr(1));
            var op = ctx.relop().GetText();
            if (!TypeTag.IsNumeric(left) || !TypeTag.IsNumeric(right))
                Report("Comparación sólo para tipos numéricos", ctx.Start);
            ExprTypes[ctx] = TypeTag.Bool;
            return TypeTag.Bool;
        }

        public override object VisitIfStmt(MiniCSParser.IfStmtContext ctx)
        {
            int cond = (int)Visit(ctx.condition());
            if (cond != TypeTag.Bool)
                Report("Condición de if debe ser bool", ctx.condition().Start);
            Visit(ctx.statement(0));
            if (ctx.ELSE() != null) Visit(ctx.statement(1));
            return 0;
        }

        public override object VisitWhileStmt(MiniCSParser.WhileStmtContext ctx)
        {
            _loopDepth++;
            int cond = (int)Visit(ctx.condition());
            if (cond != TypeTag.Bool)
                Report("Condición de while debe ser bool", ctx.condition().Start);
            Visit(ctx.statement());
            _loopDepth--;
            return TypeTag.Void;
        }

        private int VisitDesignatorAndGetType(MiniCSParser.DesignatorContext ctx)
        {
            // 1) Base: nombre de variable o parámetro
            var baseName = ctx.ident(0).GetText();
            var baseSym = Table.Lookup(baseName);
            if (baseSym == null)
            {
                Report($"Símbolo '{baseName}' no declarado", ctx.ident(0).Start);
                ExprTypes[ctx] = TypeTag.Unknown;
                return TypeTag.Unknown;
            }

            int tag = baseSym.TypeTag;

            for (int i = 0; i < ctx.DOT().Length; i++)
            {
                var fieldName = ctx.ident(i + 1).GetText();
                if (!Table.LookupField(tag, fieldName, out var fieldSym))
                {
                    Report($"Campo '{fieldName}' no existe en tipo {TypeTag.PrettyPrint(tag)}",
                        ctx.ident(i + 1).Start);
                    tag = TypeTag.Unknown;
                }
                else
                {
                    tag = fieldSym.TypeTag;
                }
            }

            for (int i = 0; i < ctx.SBL().Length; i++)
            {
                var idxExpr = ctx.expr(i);
                int idxTag = (int)Visit(idxExpr);
                if (idxTag != TypeTag.Int)
                    Report("Índice de arreglo debe ser int", idxExpr.Start);

                tag = tag % TypeTag.ListBase;
            }

            ExprTypes[ctx] = tag;
            return tag;
        }


        public override object VisitForStmt(MiniCSParser.ForStmtContext ctx)
        {
            _loopDepth++;
            if (ctx.forInit() != null) Visit(ctx.forInit());
            if (ctx.condition() != null)
            {
                int c = (int)Visit(ctx.condition());
                if (c != TypeTag.Bool)
                    Report("Condición de for debe ser bool", ctx.condition().Start);
            }

            Visit(ctx.statement());
            if (ctx.forUpdate() != null) Visit(ctx.forUpdate());
            _loopDepth--;
            return TypeTag.Void;
        }

        public override object VisitBreakStmt(MiniCSParser.BreakStmtContext ctx)
        {
            if (_loopDepth == 0)
                Report("break fuera de bucle", ctx.Start);
            return 0;
        }

        public override object VisitCast(MiniCSParser.CastContext ctx)
        {
            string typeText = ctx.type().GetText();
            int toTag = TypeTag.FromTypeNameWithBrackets(typeText);

            ExprTypes[ctx] = toTag;

            return toTag;
        }

        public override object VisitCondTerm(MiniCSParser.CondTermContext ctx)
        {
            foreach (var cf in ctx.condFact())
                Visit(cf);
            ExprTypes[ctx] = TypeTag.Bool;
            return TypeTag.Bool;
        }


        public override object VisitCondition(MiniCSParser.ConditionContext ctx)
        {
            foreach (var ct in ctx.condTerm())
                Visit(ct);
            ExprTypes[ctx] = TypeTag.Bool;
            return TypeTag.Bool;
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
            var baseName = ctx.ident(0).GetText();
            var sym = Table.Lookup(baseName);
            if (sym == null)
            {
                Report($"Símbolo '{baseName}' no declarado", ctx.ident(0).Start);
                ExprTypes[ctx] = TypeTag.Int; // fallback para no romper el resto
                return TypeTag.Int;
            }

            int tag = sym.TypeTag;

            for (int i = 0; i < ctx.DOT().Length; i++)
            {
                string fieldName = ctx.ident(i + 1).GetText();
                if (!Table.LookupField(tag, fieldName, out var fieldSym))
                {
                    Report(
                        $"Campo '{fieldName}' no encontrado en tipo {TypeTag.PrettyPrint(tag)}",
                        ctx.ident(i + 1).Start
                    );
                }
                else
                {
                    tag = fieldSym.TypeTag;
                }
            }

            for (int i = 0; i < ctx.SBL().Length; i++)
            {
                int idxTag = (int)Visit(ctx.expr(i));
                if (idxTag != TypeTag.Int)
                    Report("Índice de arreglo debe ser int", ctx.SBL(i).Symbol);

                tag = tag % TypeTag.ListBase;
            }

            ExprTypes[ctx] = tag;
            return tag;
        }


        public override object VisitAssignStmt(MiniCSParser.AssignStmtContext ctx)
        {
            var leftTag = (int)VisitDesignator(ctx.designator());
            var rightTag = (int)Visit(ctx.expr());

            if (leftTag != rightTag)
            {
                Report(
                    $"No se puede asignar {TypeTag.PrettyPrint(rightTag)} a {TypeTag.PrettyPrint(leftTag)}",
                    ctx.Start
                );
            }

            return 0;
        }

        public override object VisitReturnStmt(MiniCSParser.ReturnStmtContext ctx)
        {
            int actual = ctx.expr() != null
                ? (int)Visit(ctx.expr())
                : TypeTag.Void;

            if (actual != _currentReturnTag)
                Report(
                    $"return devuelve {TypeTag.PrettyPrint(actual)}, " +
                    $"pero el método espera {_currentReturnTag} ({TypeTag.PrettyPrint(_currentReturnTag)})",
                    ctx.Start
                );

            return TypeTag.Void;
        }


        public override object VisitReadStmt(MiniCSParser.ReadStmtContext ctx)
        {
            var tgtTag = (int)Visit(ctx.designator());

            if (tgtTag != TypeTag.Int)
                Report($"read solo puede asignar a variables de tipo int, no a {TypeTag.PrettyPrint(tgtTag)}",
                    ctx.Start);

            return TypeTag.Void;
        }


        public override object VisitWriteStmt(MiniCSParser.WriteStmtContext ctx)
        {
            var tag = (int)Visit(ctx.expr());

            switch (tag)
            {
                case TypeTag.Int:
                case TypeTag.Char:
                case TypeTag.Bool:
                case TypeTag.Float:
                case TypeTag.Double:
                case TypeTag.String:
                    break; // OK
                default:
                    Report(
                        $"write no soporta el tipo {TypeTag.PrettyPrint(tag)}",
                        ctx.Start
                    );
                    break;
            }

            if (ctx.COMMA() != null && ctx.NUMLIT() == null)
            {
                Report("La segunda posición de write debe ser un NUMLIT", ctx.Start);
            }

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