using Antlr4.Runtime;
using MiniCSharp.checker.symbolTable;
using parser;

public class SymbolTableVisitor : MiniCSParserBaseVisitor<object>
{
    public SymbolTable Table { get; } = new SymbolTable();

    // Helpers…

    // 1) Programa: abre 2 scopes (global + clase) y registra TODO
    public override object VisitProgram(MiniCSParser.ProgramContext ctx)
    {
        Table.OpenScope();   // nivel 0: global
        Table.OpenScope();   // nivel 1: clase principal

        // Registra la clase
        Table.InsertClass(ctx.ident().Start, ctx);

        // Campos y métodos de esa clase
        foreach (var vd in ctx.varDecl())    VisitVarDecl(vd);
        foreach (var md in ctx.methodDecl()) VisitMethodDecl(md);

        // ¡Nunca cierres aquí esos 2 scopes!
        return null;
    }

    // 2) Campos y variables: nivel 1 (clase) o superior (métodos / bloques)
    public override object VisitVarDecl(MiniCSParser.VarDeclContext ctx)
    {
        var tag = GetTypeTag(ctx.type().GetText(), ctx.type().Start);
        foreach (var id in ctx.ident())
            Table.InsertVariable(id.Start, tag, false, ctx);
        return null;
    }

    // 3) Métodos: firma al nivel 1, luego abre scope para parámetros (nivel 2)
   public override object VisitMethodDecl(MiniCSParser.MethodDeclContext ctx)
{
    // 1) Calculamos returnTag
    int returnTag;
    if (ctx.VOID() != null)
    {
        returnTag = TypeTags.Void;
    }
    else
    {
        var returnTypeName = ctx.type().GetText();                // p.ej. "int" o "List<string>"
        returnTag = TypeTags.FromTypeName(returnTypeName);       // mapea a TypeTags.Int, TypeTags.ListString, etc.
        if (returnTag == TypeTags.Unknown)
            ReportError($"Tipo de retorno desconocido '{returnTypeName}'", ctx.type().Start);
    }

    // 2) Calculamos paramTags
    var paramTags = new List<int>();
    if (ctx.formPars() != null)
    {
        for (int i = 0; i < ctx.formPars().ident().Length; i++)
        {
            var pTypeName = ctx.formPars().type(i).GetText();     // p.ej. "float"
            var pTag = TypeTags.FromTypeName(pTypeName);         // TypeTags.Float
            if (pTag == TypeTags.Unknown)
                ReportError($"Tipo de parámetro desconocido '{pTypeName}'", ctx.formPars().type(i).Start);
            paramTags.Add(pTag);
        }
    }

    // 3) Insertamos el método en el nivel de la clase
    var methodTok = ctx.ident().Start;
    if (!Table.InsertMethod(methodTok, returnTag, paramTags, ctx))
        ReportError($"Método '{methodTok.Text}' redeclarado", methodTok);

    // 4) Abrimos scope para parámetros y locales
    Table.OpenScope();

    // 5) Insertamos los parámetros como variables en nivel 2
    if (ctx.formPars() != null)
    {
        for (int i = 0; i < ctx.formPars().ident().Length; i++)
        {
            var idTok = ctx.formPars().ident(i).Start;
            var pTag  = paramTags[i];
            if (!Table.InsertVariable(idTok, pTag, isConstant: false, ctx.formPars()))
                ReportError($"Parámetro '{idTok.Text}' duplicado", idTok);
        }
    }

    // 6) Procesamos el cuerpo (las varDecl dentro de VisitBlock insertarán las locales)
    Visit(ctx.block());

    // 7) Cerramos el scope del método
    Table.CloseScope();

    return null;
}


    // 4) Bloques internos (if, for, while, {...}): si quieres scopes aislados, hazlo
    public override object VisitBlock(MiniCSParser.BlockContext ctx)
    {
        foreach (var v in ctx.varDecl())    VisitVarDecl(v);
        foreach (var s in ctx.statement())  Visit(s);
        return null;
    }
    
    private int GetTypeTag(string typeName, IToken tok)
    {
        var tag = TypeTags.FromTypeName(typeName);
        if (tag == TypeTags.Unknown)
            ReportError($"Tipo '{typeName}' desconocido", tok);
        return tag;
    }
    
    private void ReportError(string msg, IToken tok)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error semántico: {msg} (línea {tok.Line}, col {tok.Column})");
        Console.ResetColor();
    }

    

}
