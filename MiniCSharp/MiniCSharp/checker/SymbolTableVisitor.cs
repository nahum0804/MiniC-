using Antlr4.Runtime;
using generated.parser;

namespace MiniCSharp.checker;

public class SymbolTableVisitor : MiniCSParserBaseVisitor<object>
{
    public SymbolTable Table { get; } = new SymbolTable();


    public override object VisitProgram(MiniCSParser.ProgramContext context)
    {
        Table.OpenScope();

        var classToken = context.ident().Start;
        if (!Table.InsertVariable(classToken, typeTag: -2, isConstant: true, context))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"Error semántico: clase '{classToken.Text}' redeclarada en el mismo scope (línea {classToken.Line}).");
            Console.ResetColor();
        }

        Table.OpenScope();

        foreach (var vdc in context.varDecl())
        {
            VisitVarDecl(vdc);
        }

        foreach (var mdc in context.methodDecl())
        {
            VisitMethodDecl(mdc);
        }

        foreach (var mdc in context.classDecl())
        {
            VisitClassDecl(mdc);
        }

        Table.CloseScope();

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
            if (!ok)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"Error semántico: variable '{idToken.Text}' redeclarada en el mismo scope (línea {idToken.Line}).");
                Console.ResetColor();
            }
        }
        return null;
    }


    public override object VisitClassDecl(MiniCSParser.ClassDeclContext context)
    {
        var classToken = context.ident().Start;
        if (!Table.InsertVariable(classToken, typeTag: -2, isConstant: true, context))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"Error semántico: clase '{classToken.Text}' redeclarada en el mismo scope (línea {classToken.Line}).");
            Console.ResetColor();
        }
        
        Table.OpenScope();
        
        foreach (var fieldDecl in context.varDecl())
        {
            VisitVarDecl(fieldDecl);
        }

        foreach (var mtdDecl in context.methodDecl())
        {
            VisitMethodDecl(mtdDecl);
        }

        Table.CloseScope();

        return null;
    }

    public override object VisitMethodDecl(MiniCSParser.MethodDeclContext context)
        {
            var methodName = context.ident().GetText();
            int returnTypeTag = context.VOID() != null
                ? -1
                : GetTypeTag(context.type().GetText());
            
            var paramTypeTags = new List<int>();
            var formParsCtx = context.formPars();
            if (formParsCtx != null)
            {
                for (int i = 0; i < formParsCtx.ident().Length; i++)
                {
                    var ptype = formParsCtx.type(i).GetText();
                    paramTypeTags.Add(GetTypeTag(ptype));
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
                for (int i = 0; i < formParsCtx.ident().Length; i++)
                {
                    var pTok = formParsCtx.ident(i).Start;
                    var pTypeTag = GetTypeTag(formParsCtx.type(i).GetText());
                    var okParam = Table.InsertVariable(pTok, pTypeTag, isConstant: false, formParsCtx);
                    if (!okParam)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(
                            $"Error semántico: parámetro '{pTok.Text}' duplicado en el método '{methodName}' (línea {pTok.Line}).");
                        Console.ResetColor();
                    }
                }
            }
            
            Visit(context.block());
            
            Table.CloseScope();
            return null;
        }

    public override object VisitBlock(MiniCSParser.BlockContext context)
    {
        Table.OpenScope(); 

        foreach (var child in context.children)
        {
            switch (child)
            {
                case MiniCSParser.VarDeclContext vdc:
                    VisitVarDecl(vdc);
                    break;
                case MiniCSParser.StatementContext stCtx:
                    VisitStatement(stCtx);
                    break;
            }
        }
        
        return null;
    }

    public override object VisitDesignator(MiniCSParser.DesignatorContext context)
    {
        var name = context.ident(0).Start.Text;
        var symbol = Table.Lookup(name);
        if (symbol != null) return null;
        var tok = context.ident(0).Start;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(
            $"Error semántico: símbolo '{name}' no declarado (línea {tok.Line}, columna {tok.Column}).");
        Console.ResetColor();

        return null;
    }


    public override object VisitStatement(MiniCSParser.StatementContext context)
    {
        if (context.designator() != null)
        {
            VisitDesignator(context.designator());
            // SE PUEDE CHEQUEAR QUE EL TIPO DE EXPR SEA COMPATIBLE
            if (context.expr() != null)
                VisitExpr(context.expr());
        }

        //Chequear expressions
        if (context.WRITE() != null && context.expr() != null)
        {
            VisitExpr(context.expr());
        }

        //Semantic extra para if,while,for
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
        else if (context.LEFTP() != null && context.expr() != null && context.RIGHTP() != null)
        {
            VisitExpr(context.expr());
        }
        else if (context.NEW() != null && context.ident() != null)
        {
            //Chequear que el tipo este declarado
        }

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

    private static int GetTypeTag(string typeName)
    {
        return typeName switch
        {
            "int" => 0,
            "char" => 1,
            "bool" => 2,
            _ => -999
        };
    }
}