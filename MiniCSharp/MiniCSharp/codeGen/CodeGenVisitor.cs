using System.Reflection;
using System.Reflection.Emit;
using MiniCSharp.checker;

namespace MiniCSharp.codeGen;

public class CodeGenVisitor : MiniCSParserBaseVisitor<object>
{
    private readonly ModuleBuilder _module;
    private TypeBuilder _currentType;
    private ILGenerator _il;
    private readonly SymbolTable _symbols;
    private readonly Dictionary<string, LocalBuilder> _locals = new();

    public CodeGenVisitor(ModuleBuilder module, SymbolTable symbols)
    {
        _module = module;
        _symbols = symbols;
    }

    public void Generate(MiniCSParser.ProgramContext ctx)
    {
        // 1) Definir tipo P
        _currentType = _module.DefineType("P", TypeAttributes.Public | TypeAttributes.Class);

        // 2) Para cada clase anidada (opcional) …
        // 3) Para cada método:
        foreach (var m in ctx.methodDecl())
            VisitMethodDecl(m);

        _currentType.CreateType();
    }

    public override object VisitMethodDecl(MiniCSParser.MethodDeclContext ctx)
    {
        var name = ctx.ident().GetText();
        var mb = _currentType.DefineMethod(
            name,
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(void),
            Type.EmptyTypes
        );
        _il = mb.GetILGenerator();

        // ¡Limpiamos el diccionario para este método!
        _locals.Clear();

        // 4) Declarar locales y guardarlos
        foreach (var vd in ctx.block().varDecl())
        {
            var varName = vd.ident(0).GetText();
            var typeClr = MapType(vd.type());

            // Guardamos el LocalBuilder que necesitamos luego
            var lb = _il.DeclareLocal(typeClr);
            _locals[varName] = lb;
        }

        // 5) Generar cuerpo
        Visit(ctx.block());

        _il.Emit(OpCodes.Ret);
        return null;
    }

    public override object VisitBlock(MiniCSParser.BlockContext ctx)
    {
        foreach (var stmt in ctx.statement())
            Visit(stmt);
        return null;
    }

    public override object VisitAssignStmt(MiniCSParser.AssignStmtContext ctx)
    {
        // 1) Generar la expresión del lado derecho
        Visit(ctx.expr());

        // 2) Averiguar nombre de variable
        var name = ctx.designator().GetText();

        // 3) Recuperar su LocalBuilder desde el diccionario
        if (!_locals.TryGetValue(name, out var lb))
            throw new InvalidOperationException($"Variable '{name}' no declarada en este método.");

        // 4) Emitir stloc con el índice sin ambigüedad
        _il.Emit(OpCodes.Stloc, lb.LocalIndex);

        return null;
    }

    public override object VisitWhileStmt(MiniCSParser.WhileStmtContext ctx)
    {
        var start = _il.DefineLabel();
        var end = _il.DefineLabel();

        _il.MarkLabel(start);
        Visit(ctx.condition());
        _il.Emit(OpCodes.Brfalse, end);

        // Visita la sentencia dentro del while, sea un bloque u otra cosa
        Visit(ctx.statement());

        _il.Emit(OpCodes.Br, start);
        _il.MarkLabel(end);
        return null;
    }


    public override object VisitExpr(MiniCSParser.ExprContext ctx)
    {
        Visit(ctx.term(0));
        for (int i = 1; i < ctx.term().Length; i++)
        {
            Visit(ctx.term(i));
            var op = ctx.addop(i - 1).GetText();
            switch (op)
            {
                case "+": _il.Emit(OpCodes.Add); break;
                case "-": _il.Emit(OpCodes.Sub); break;
                default: throw new InvalidOperationException($"Operador inesperado: {op}");
            }
        }

        return null;
    }

    public override object VisitFactor(MiniCSParser.FactorContext ctx)
    {
        // 1) Caso literal entero
        if (ctx.NUMLIT() != null)
        {
            int val = int.Parse(ctx.NUMLIT().GetText());
            _il.Emit(OpCodes.Ldc_I4, val);
            return null;
        }

        // 2) Caso una variable simple: designator sin llamada a método
        if (ctx.designator() != null && ctx.LEFTP() == null)
        {
            var name = ctx.designator().GetText();
            if (!_locals.TryGetValue(name, out var lb))
                throw new InvalidOperationException($"Variable '{name}' no declarada.");

            _il.Emit(OpCodes.Ldloc, lb.LocalIndex);
            return null;
        }

        // 3) Paréntesis: ( expr )
        if (ctx.LEFTP() != null && ctx.expr().Length > 0)
        {
            // Sólo tomamos la primera expr dentro de ()
            Visit(ctx.expr(0));
            return null;
        }

        // 4) Aquí podrías añadir FLOATLIT, DOUBLELIT, CHARLIT, STRINGLIT, TRUE/FALSE…
        throw new NotSupportedException($"Factor no soportado: {ctx.GetText()}");
    }

    public override object VisitWriteStmt(MiniCSParser.WriteStmtContext ctx)
    {
        // Generar código de la expresión que se imprime
        Visit(ctx.expr());

        // Console.Write(int)
        var writeInt = typeof(Console).GetMethod("WriteLine", new[] { typeof(int) })!;
        _il.EmitCall(OpCodes.Call, writeInt, null);

        return null;
    }
    
    public override object VisitReadStmt(MiniCSParser.ReadStmtContext ctx)
    {
        // Console.ReadLine()
        _il.EmitCall(
            OpCodes.Call,
            typeof(Console).GetMethod("ReadLine", Type.EmptyTypes)!,
            null
        );

        // int.Parse(...)
        _il.EmitCall(
            OpCodes.Call,
            typeof(int).GetMethod("Parse", new[]{ typeof(string) })!,
            null
        );

        // Guardar en la variable destino
        var name = ctx.designator().GetText();
        var lb   = _locals[name];  // ya lo declaraste en VisitMethodDecl
        _il.Emit(OpCodes.Stloc, lb.LocalIndex);

        return null;
    }

    
    public override object VisitCallStmt(MiniCSParser.CallStmtContext ctx)
    {
        var name = ctx.designator().GetText();
        var args = ctx.actPars()?.expr()
                   ?? Array.Empty<MiniCSParser.ExprContext>();

        // Ejemplo simple: llamar a un método static de tu clase
        // (más adelante podrías buscarlo en tu tabla de símbolos y emitir Call)
        throw new NotSupportedException($"Llamada a '{name}' no implementada.");
    }


    // … más overrides para llamadas, if, new arrays, read, write, len, ord, chr …

    private Type MapType(MiniCSParser.TypeContext t)
    {
        return t.GetText() switch
        {
            "int" => typeof(int),
            "char" => typeof(char),
            "string" => typeof(string),
            _ => throw new NotSupportedException(t.GetText())
        };
    }
}