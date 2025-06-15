using MiniCSharp.utils;

namespace MiniCSharp.checker;

public class SymbolTableVisitor : MiniCSParserBaseVisitor<object>
{
    public SymbolTable Table { get; } = new();
    public static SymbolTable? CurrentSymbolTable { get; private set; }

    public SymbolTableVisitor()
    {
        CurrentSymbolTable = Table;
    }

    public override object VisitProgram(MiniCSParser.ProgramContext context)
    {
        Table.OpenScope();
        foreach (var c in context.classDecl())
            VisitClassDecl(c);

        foreach (var v in context.varDecl())
            VisitVarDecl(v);

        foreach (var m in context.methodDecl())
            VisitMethodDecl(m);

        return null;
    }


    public override object VisitVarDecl(MiniCSParser.VarDeclContext context)
    {
        var typeName = context.type().GetText();
        var typeTag = GetTypeTag(typeName);

        foreach (var idCtx in context.ident())
        {
            var idToken = idCtx.Start;
            var ok = Table.InsertVariable(idToken, typeTag, isConstant: false, context);
            if (ok) continue;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"Error semántico: variable '{idToken.Text}' redeclarada en el mismo scope (línea {idToken.Line}).");
            Console.ResetColor();
        }

        return null;
    }


    public override object VisitMethodDecl(MiniCSParser.MethodDeclContext context)
    {
        var methodName = context.ident().GetText();
        int returnTypeTag;

        if (context.VOID() != null)
        {
            returnTypeTag = -1;
        }
        else
        {
            var retTypeName = context.type().GetText();
            returnTypeTag = GetTypeTag(retTypeName);
        }

        var paramTypeTags = new List<int>();
        var formParsCtx = context.formPars();
        if (formParsCtx != null)
        {
            for (var i = 0; i < formParsCtx.ident().Length; i++)
            {
                var ptype = formParsCtx.type(i).GetText();
                var pTag = GetTypeTag(ptype);
                paramTypeTags.Add(pTag);
            }
        }

        var methodToken = context.ident().Start;
        var okMethod = Table.InsertMethod(methodToken, returnTypeTag, paramTypeTags, context);
        if (!okMethod)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"Error semántico: método '{methodName}' redeclarado en el mismo scope (línea {methodToken.Line}).");
            Console.ResetColor();
        }

        Table.OpenScope();

        if (formParsCtx != null)
        {
            for (var i = 0; i < formParsCtx.ident().Length; i++)
            {
                var pTok = formParsCtx.ident(i).Start;
                var pTypeName = formParsCtx.type(i).GetText();
                var pTypeTag = GetTypeTag(pTypeName);

                var okParam = Table.InsertVariable(pTok, pTypeTag, isConstant: false, formParsCtx);
                if (okParam) continue;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"Error semántico: parámetro '{pTok.Text}' duplicado en el método '{methodName}' (línea {pTok.Line}).");
                Console.ResetColor();
            }
        }

        Visit(context.block());


        return null;
    }

    public override object VisitBlock(MiniCSParser.BlockContext context)
    {
        Table.OpenScope();

        foreach (var item in context.children)
            Visit(item);

        return null;
    }


    public override object VisitDesignator(MiniCSParser.DesignatorContext context)
    {
        var baseName = context.ident(0).GetText();

        if (baseName is "add" or "len" or "del")
        {
            return null;
        }

        var symbol = Table.Lookup(baseName);

        if (symbol == null)
        {
            var tok = context.ident(0).Start;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"Error semántico: símbolo '{baseName}' no declarado (línea {tok.Line}, columna {tok.Column}).");
            Console.ResetColor();
            return null;
        }

        if (context.DOT() != null && context.ident().Length > 1)
        {
            var field = context.ident(1).GetText();

            if (symbol.TypeTag >= 200 &&
                TypeTag.ClassNameFromTag(symbol.TypeTag) is string)
            {
                if (!Table.LookupField(symbol.TypeTag, field, out _))
                {
                    var tok = context.ident(1).Start;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        $"Error semántico: campo '{field}' no encontrado en el tipo del objeto '{baseName}' (línea {tok.Line}, col {tok.Column}).");
                    Console.ResetColor();
                }
            }
            else
            {
                var tok = context.ident(0).Start;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"Error semántico: '{baseName}' no es un objeto de clase válido (línea {tok.Line}, columna {tok.Column}).");
                Console.ResetColor();
            }
        }

        foreach (var expr in context.expr())
        {
            VisitExpr(expr);
        }

        return null;
    }

    public override object VisitAssignStmt(MiniCSParser.AssignStmtContext context)
    {
        VisitDesignator(context.designator());
        VisitExpr(context.expr());
        return null;
    }

    public override object VisitReadStmt(MiniCSParser.ReadStmtContext context)
    {
        VisitDesignator(context.designator());
        return null;
    }

    public override object VisitWriteStmt(MiniCSParser.WriteStmtContext context)
    {
        VisitExpr(context.expr());
        return null;
    }


    public override object VisitExpr(MiniCSParser.ExprContext context)
    {
        for (var i = 0; i < context.term().Length; i++)
        {
            VisitTerm(context.term(i));
        }

        return null;
    }

    public override object VisitTerm(MiniCSParser.TermContext context)
    {
        foreach (var fCtx in context.factor())
        {
            VisitFactor(fCtx);
        }

        return null;
    }


    public override object VisitFactor(MiniCSParser.FactorContext context)
    {
        if (context.designator() != null)
        {
            VisitDesignator(context.designator());
            if (context.LEFTP() != null && context.actPars() != null)
                VisitActPars(context.actPars());
        }

        else if (context.NUMLIT() != null)
            return TypeTag.Int;

        else if (context.FLOATLIT() != null)
            return TypeTag.Float;

        else if (context.DOUBLELIT() != null)
            return TypeTag.Double;

        else if (context.CHARLIT() != null)
            return TypeTag.Char;

        else if (context.STRINGLIT() != null)
            return TypeTag.String;

        else if (context.TRUE() != null || context.FALSE() != null)
            return TypeTag.Bool;


        else if (context.LEFTP() != null && context.RIGHTP() != null && context.expr().Length == 1)
        {
            VisitExpr(context.expr(0));
        }
        else if (context.NEW() != null && context.ident() != null)
        {
            foreach (var expr in context.expr())
            {
                VisitExpr(expr);
            }

            var typeName = context.ident().GetText();
            var typeTag =
                TypeTag.FromTypeNameWithBrackets(typeName +
                                                 string.Concat(Enumerable.Repeat("[]", context.expr().Length)));

            if (typeTag == TypeTag.Unknown)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"Error semántico: tipo '{typeName}' no declarado o inválido (línea {context.ident().Start.Line}).");
                Console.ResetColor();
            }
        }

        return null;
    }

    public override object VisitClassDecl(MiniCSParser.ClassDeclContext ctx)
    {
        var className = ctx.ident().GetText();
        var classToken = ctx.ident().Start;

        int classTag = TypeTag.RegisterClass(className);

        if (!Table.InsertClass(classToken, classTag, ctx))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"Clase '{className}' redeclarada", classToken);
            Console.ResetColor();
        }

        Table.OpenScope();
        foreach (var v in ctx.varDecl())
        {
            var fieldTypeTag = GetTypeTag(v.type().GetText());
            foreach (var id in v.ident())
            {
                if (!Table.InsertField(className, id.Start, fieldTypeTag))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        $"Campo '{id.GetText()}' redeclarado en clase '{className}'", id.Start);
                    Console.ResetColor();
                }
            }
        }

        Table.CloseScope();

        return null;
    }


    public override object VisitActPars(MiniCSParser.ActParsContext context)
    {
        foreach (var eCtx in context.expr())
        {
            VisitExpr(eCtx);
        }

        return null;
    }

    private static int GetTypeTag(string typeText)
    {
        return TypeTag.FromTypeNameWithBrackets(typeText);
    }
}