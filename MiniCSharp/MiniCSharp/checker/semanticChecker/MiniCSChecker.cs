using Antlr4.Runtime;
using generated.parser;
using MiniCSharp.checker.symbolTable;

namespace MiniCSharp.checker.semanticChecker
{
    public class MiniCsChecker(SymbolTable table) : MiniCSParserBaseVisitor<object>
    {
        public SymbolTable Table { get; } = table;
        private readonly List<string> _errors = [];

        public IEnumerable<string> Errors => _errors;
        public bool HasErrors => _errors.Count > 0;

        private int _loopDepth = 0;

        private void Report(string msg, IToken tok)
        {
            _errors.Add($"Error semántico: {msg} '{tok.Text}' (línea {tok.Line}, col {tok.Column})");
        }

        private static readonly HashSet<string> ReservedKeywords = new()
        {
            "class", "void", "if", "else", "while", "for", "return", "break",
            "read", "write", "switch", "using", "default", "case", "List",
            "new", "true", "false", "int", "char", "bool", "string", "float"
        };

        public override object VisitAssignStatement(MiniCSParser.AssignStatementContext ctx)
        {
            var lhsTag = (int)VisitDesignator(ctx.designator());
            var rhsTag = (int)VisitExpr(ctx.expr());

            if (lhsTag != rhsTag)
                Report($"Incompatibilidad de tipos en asignación (se esperaba {TypeTags.Name(lhsTag)}, vino {TypeTags.Name(rhsTag)})",
                    ctx.ASSIGN().Symbol);

            return null;
        }


        public override object VisitBlock(MiniCSParser.BlockContext ctx)
        {
            Table.OpenScope();

            foreach (var vdc in ctx.varDecl())
            {
                Visit(vdc); // Esto usa VisitVarDecl para registrar variables locales
            }

            foreach (var stmt in ctx.statement())
            {
                Visit(stmt); // Procesa los statements dentro del bloque
            }

            Table.CloseScope();
            return null;
        }

        public override object VisitVarDecl(MiniCSParser.VarDeclContext ctx)
        {
            // sólo comprobamos que el tipo exista
            var typeTag = (int)Visit(ctx.type());
            if (typeTag == TypeTags.Unknown)
                Report($"Tipo no declarado '{ctx.type().GetText()}'", ctx.type().Start);
            return null;
        }

        public override object VisitDesignator(MiniCSParser.DesignatorContext ctx)
        {
            var name = ctx.ident(0).GetText();
            Console.WriteLine($"[DEBUG] Lookup de '{name}'. Símbolos activos:");
            Table.PrintActive();
            var symbol = Table.Lookup(name);

            if (symbol == null)
            {
                Report($"Símbolo no declarado '{name}'", ctx.ident(0).Start);
                return TypeTags.Unknown;
            }

            // Acceso con índices: listas (p.ej. enteros[i])
            if (ctx.SBL().Length > 0)
            {
                if (!TypeTags.IsList(symbol.TypeTag))
                {
                    Report($"Símbolo '{name}' no es una lista", ctx.ident(0).Start);
                    return TypeTags.Unknown;
                }

                foreach (var expr in ctx.expr())
                {
                    var indexType = (int)VisitExpr(expr);
                    if (indexType != TypeTags.Int)
                    {
                        Report("Índice de lista debe ser de tipo int", expr.Start);
                    }
                }

                return TypeTags.ElementType(symbol.TypeTag);
            }

            return symbol.TypeTag;
        }


        public override object VisitReturnStatement(MiniCSParser.ReturnStatementContext ctx)
        {
            if (ctx.expr() != null)
                _ = VisitExpr(ctx.expr());
            return null;
        }

        public override object VisitExpr(MiniCSParser.ExprContext ctx)
        {
            var type = (int)VisitTerm(ctx.term(0));
            for (var i = 1; i < ctx.term().Length; i++)
            {
                var op = ctx.addop(i - 1).GetText();
                var rhs = (int)VisitTerm(ctx.term(i));
                type = TypeChecker.Compatible(type, rhs, op);
                if (type == TypeTags.Unknown)
                    Report($"Operador '{op}' no válido entre tipos", ctx.addop(i - 1).Start);
            }

            return type;
        }

        public override object VisitTerm(MiniCSParser.TermContext ctx)
        {
            var type = (int)VisitFactor(ctx.factor(0));
            for (var i = 1; i < ctx.factor().Length; i++)
            {
                var op = ctx.mulop(i - 1).GetText();
                var rhs = (int)VisitFactor(ctx.factor(i));
                type = TypeChecker.Compatible(type, rhs, op);
                if (type == TypeTags.Unknown)
                    Report($"Operador '{op}' no válido entre tipos", ctx.mulop(i - 1).Start);
            }

            return type;
        }

        public override object VisitFactor(MiniCSParser.FactorContext ctx)
        {
            if (ctx.FLOATLIT() != null) return TypeTags.Float;
            if (ctx.listLiteral() != null)
                return VisitListLiteral(ctx.listLiteral());
            if (ctx.NUMLIT() != null) return TypeTags.Int;
            if (ctx.CHARLIT() != null) return TypeTags.Char;
            if (ctx.STRINGLIT() != null) return TypeTags.String;
            if (ctx.TRUE() != null || ctx.FALSE() != null) return TypeTags.Bool;

            if (ctx.designator() != null)
            {
                var sym = Table.Lookup(ctx.designator().GetText());
                return sym?.TypeTag ?? ReportAndReturn(TypeTags.Unknown, ctx.designator().Start);
            }

            return ctx.LEFTP() != null ? VisitExpr(ctx.expr()) : TypeTags.Unknown;
        }

        public override object VisitCondition(MiniCSParser.ConditionContext ctx)
        {
            _ = Visit(ctx.condTerm(0));
            for (var i = 1; i < ctx.condTerm().Length; i++)
            {
                var t2 = (int)Visit(ctx.condTerm(i));
                if (t2 != TypeTags.Bool)
                    Report("Operador '||' aplicado a no-bool", ctx.OR(i - 1).Symbol);
            }

            return TypeTags.Bool;
        }

        public override object VisitCondTerm(MiniCSParser.CondTermContext ctx)
        {
            _ = Visit(ctx.condFact(0));
            for (var i = 1; i < ctx.condFact().Length; i++)
            {
                var t2 = (int)Visit(ctx.condFact(i));
                if (t2 != TypeTags.Bool)
                    Report("Operador '&&' aplicado a no-bool", ctx.AND(i - 1).Symbol);
            }

            return TypeTags.Bool;
        }

        public override object VisitCondFact(MiniCSParser.CondFactContext ctx)
        {
            var left = (int)Visit(ctx.expr(0));
            var right = (int)Visit(ctx.expr(1));
            var op = ctx.relop().GetText();

            var result = TypeChecker.Compatible(left, right, op);
            if (result != TypeTags.Bool)
                Report($"Operador relacional '{op}' no válido entre tipos", ctx.relop().Start);

            return TypeTags.Bool;
        }

        public override object VisitListLiteral(MiniCSParser.ListLiteralContext ctx)
        {
            var elemType = (int)VisitExpr(ctx.expr(0));
            if (elemType == TypeTags.Unknown)
            {
                Report("Tipo de elemento no reconocido en la lista", ctx.expr(0).Start);
                return TypeTags.Unknown;
            }

            for (var i = 1; i < ctx.expr().Length; i++)
            {
                var t = (int)VisitExpr(ctx.expr(i));
                if (t != elemType)
                    Report("Elementos de lista con tipos distintos", ctx.expr(i).Start);
            }

            return TypeTags.ListOf(elemType);
        }

        public override object VisitIfStatement(MiniCSParser.IfStatementContext ctx)
        {
            var condTag = (int)Visit(ctx.condition());
            if (condTag != TypeTags.Bool)
                Report("Condición de IF no booleana", ctx.IF().Symbol);

            Visit(ctx.statement(0));
            if (ctx.ELSE() != null) Visit(ctx.statement(1));
            return null;
        }

        public override object VisitWhileStatement(MiniCSParser.WhileStatementContext ctx)
        {
            _loopDepth++;
            var condTag = (int)Visit(ctx.condition());
            if (condTag != TypeTags.Bool)
                Report("Condición de WHILE no booleana", ctx.WHILE().Symbol);
            Visit(ctx.statement());
            _loopDepth--;
            return null;
        }


        public override object VisitForStatement(MiniCSParser.ForStatementContext ctx)
        {
            Table.OpenScope(); // Iniciar nuevo scope (por si se declara una variable en el for)

            // Parte de inicialización
            if (ctx.forInit() != null)
                Visit(ctx.forInit());

            // Condición del for
            if (ctx.condition() != null)
            {
                var condTag = (int)Visit(ctx.condition());
                if (condTag != TypeTags.Bool)
                    Report("Condición de FOR no booleana", ctx.condition().Start);
            }

            // Actualización (al final de cada iteración)
            if (ctx.forUpdate() != null)
                Visit(ctx.forUpdate());

            _loopDepth++;
            Visit(ctx.statement()); // cuerpo del for
            _loopDepth--;

            Table.CloseScope(); // Cerrar scope del for

            return null;
        }


        public override object VisitBreakStatement(MiniCSParser.BreakStatementContext ctx)
        {
            if (_loopDepth == 0)
                Report("Break fuera de bucle", ctx.BREAK().Symbol);
            return null;
        }

        public override object VisitIntType(MiniCSParser.IntTypeContext ctx)
        {
            return TypeTags.Int;
        }

        public override object VisitCharType(MiniCSParser.CharTypeContext ctx)
        {
            return TypeTags.Char;
        }

        public override object VisitBoolType(MiniCSParser.BoolTypeContext ctx)
        {
            return TypeTags.Bool;
        }

        public override object VisitStringType(MiniCSParser.StringTypeContext ctx)
        {
            return TypeTags.String;
        }

        public override object VisitFloatType(MiniCSParser.FloatTypeContext ctx)
        {
            return TypeTags.Float;
        }

        public override object VisitSimpletype(MiniCSParser.SimpletypeContext ctx)
        {
            // Esta alternativa es 'type → simpleType'
            // simpleType() devuelve un SimpleTypeContext (uno de los cinco anteriores)
            return Visit(ctx.simpleType());
        }

        public override object VisitListOfSimple(MiniCSParser.ListOfSimpleContext ctx)
        {
            // 'type → LIST '<' simpleType '>''
            var elemTag = (int)Visit(ctx.simpleType());
            return elemTag == TypeTags.Unknown
                ? TypeTags.Unknown
                : TypeTags.ListOf(elemTag);
        }

        public override object VisitUserTypeOrArray(MiniCSParser.UserTypeOrArrayContext ctx)
        {
            // 'type → ident ( '[' ']' )?'
            var name = ctx.ident().GetText();
            var sym = Table.Lookup(name);

            if (sym is ClassSymbol)
            {
                if (ctx.SBL() != null)
                {
                    // Si quieres soportar arrays de clases, aquí podrías asignarles un tag aparte.
                    Report($"Arrays de clase no soportados", ctx.Start);
                    return TypeTags.Unknown;
                }

                return TypeTags.Class;
            }

            Report($"Tipo no declarado '{name}'", ctx.ident().Start);
            return TypeTags.Unknown;
        }


        public override object VisitReadStatement(MiniCSParser.ReadStatementContext ctx)
        {
            var tag = (int)VisitDesignator(ctx.designator());
            if (tag != TypeTags.Int)
                Report("READ sólo admite variables int", ctx.READ().Symbol);
            return null;
        }

        public override object VisitWriteStatement(MiniCSParser.WriteStatementContext ctx)
        {
            _ = VisitExpr(ctx.expr());
            return null;
        }
        
        private int ReportAndReturn(int tag, IToken tok)
        {
            Report("Símbolo no declarado", tok);
            return tag;
        }
    }
}