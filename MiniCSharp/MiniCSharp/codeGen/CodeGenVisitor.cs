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
        _currentType = _module.DefineType("P", TypeAttributes.Public | TypeAttributes.Class);

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

        _locals.Clear();

        foreach (var vd in ctx.block().varDecl())
        {
            foreach (var id in vd.ident())
            {
                var varName = id.GetText();
                var typeClr = MapType(vd.type());

                var lb = _il.DeclareLocal(typeClr);
                _locals[varName] = lb;
            }
        }

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
        Visit(ctx.expr());

        var name = ctx.designator().GetText();

        if (!_locals.TryGetValue(name, out var lb))
            throw new InvalidOperationException($"Variable '{name}' no declarada en este método.");

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
        if (ctx.NUMLIT() != null)
        {
            int val = int.Parse(ctx.NUMLIT().GetText());
            _il.Emit(OpCodes.Ldc_I4, val);
            return null;
        }

        if (ctx.designator() != null && ctx.LEFTP() == null)
        {
            var name = ctx.designator().GetText();
            if (!_locals.TryGetValue(name, out var lb))
                throw new InvalidOperationException($"Variable '{name}' no declarada.");

            _il.Emit(OpCodes.Ldloc, lb.LocalIndex);
            return null;
        }

        if (ctx.LEFTP() != null && ctx.expr().Length > 0)
        {
            Visit(ctx.expr(0));
            return null;
        }

        throw new NotSupportedException($"Factor no soportado: {ctx.GetText()}");
    }

    public override object VisitWriteStmt(MiniCSParser.WriteStmtContext ctx)
    {
        Visit(ctx.expr());

        var writeInt = typeof(Console).GetMethod("WriteLine", new[] { typeof(int) })!;
        _il.EmitCall(OpCodes.Call, writeInt, null);

        return null;
    }
    
    public override object VisitReadStmt(MiniCSParser.ReadStmtContext ctx)
    {
        _il.EmitCall(
            OpCodes.Call,
            typeof(Console).GetMethod("ReadLine", Type.EmptyTypes)!,
            null
        );

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

    public override object VisitIfStmt(MiniCSParser.IfStmtContext ctx)
    {
        Visit(ctx.condition());                      // pila: 0 o 1
        var elseL = _il.DefineLabel();
        var endL  = _il.DefineLabel();
        _il.Emit(OpCodes.Brfalse, elseL);

        Visit(ctx.statement(0));                     // then
        _il.Emit(OpCodes.Br, endL);

        _il.MarkLabel(elseL);
        if (ctx.ELSE() != null)
            Visit(ctx.statement(1));                 // else

        _il.MarkLabel(endL);
        return null;
    }
    
    public override object VisitCondFact(MiniCSParser.CondFactContext ctx)
    {
        // expr relop expr
        Visit(ctx.expr(0));
        Visit(ctx.expr(1));

        // Emite la comparación adecuada
        switch (ctx.relop().GetText())
        {
            case "==": _il.Emit(OpCodes.Ceq);        break;
            case "!=": _il.Emit(OpCodes.Ceq);        _il.Emit(OpCodes.Ldc_I4_0); _il.Emit(OpCodes.Ceq); break;
            case "<":  _il.Emit(OpCodes.Clt);        break;
            case ">":  _il.Emit(OpCodes.Cgt);        break;
            case "<=": _il.Emit(OpCodes.Cgt);        _il.Emit(OpCodes.Ldc_I4_0); _il.Emit(OpCodes.Ceq); break;
            case ">=": _il.Emit(OpCodes.Clt);        _il.Emit(OpCodes.Ldc_I4_0); _il.Emit(OpCodes.Ceq); break;
            default: throw new NotSupportedException($"Relop no soportado: {ctx.relop().GetText()}");
        }

        return null;
    }

    public override object VisitCondTerm(MiniCSParser.CondTermContext ctx)
    {
        // condFact ( AND condFact )*
        Visit(ctx.condFact(0));
        for (int i = 1; i < ctx.condFact().Length; i++)
        {
            Visit(ctx.condFact(i));
            _il.Emit(OpCodes.And);
        }
        return null;
    }

    public override object VisitCondition(MiniCSParser.ConditionContext ctx)
    {
        // condTerm ( OR condTerm )*
        Visit(ctx.condTerm(0));
        for (int i = 1; i < ctx.condTerm().Length; i++)
        {
            Visit(ctx.condTerm(i));
            _il.Emit(OpCodes.Or);
        }
        return null;
    }


    public override object VisitCallStmt(MiniCSParser.CallStmtContext ctx)
    {
        var name = ctx.designator().GetText();
        var args = ctx.actPars()?.expr()
                   ?? Array.Empty<MiniCSParser.ExprContext>();

        throw new NotSupportedException($"Llamada a '{name}' no implementada.");
    }



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