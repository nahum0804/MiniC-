using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using MiniCSharp.checker;
using MiniCSharp.utils;

namespace MiniCSharp.codeGen;

public class CodeGenVisitor : MiniCSParserBaseVisitor<object>
{
    private readonly ModuleBuilder _module;
    private TypeBuilder _currentType;
    private ILGenerator _il;
    private readonly SymbolTable _symbols;
    private readonly Dictionary<string, LocalBuilder> _locals = new();

    private readonly Dictionary<(string className, string fieldName), FieldBuilder> _fieldBuilders
        = new();

    private readonly Dictionary<string, ConstructorBuilder> _ctorBuilders
        = new();

    private readonly Dictionary<string, TypeBuilder> _nestedTypes
        = new();

    private readonly Dictionary<ParserRuleContext, int> _exprTypes;


    public CodeGenVisitor(ModuleBuilder module, SymbolTable symbols,
        Dictionary<ParserRuleContext, int> exprTypes)
    {
        _module = module;
        _symbols = symbols;
        _exprTypes = exprTypes;
    }

    public void Generate(MiniCSParser.ProgramContext ctx)
    {
        _currentType = _module.DefineType(
            ctx.ident().GetText(),
            TypeAttributes.Public | TypeAttributes.Class);


        foreach (var cd in ctx.classDecl())
        {
            var className = cd.ident().GetText();
            var nested = _currentType.DefineNestedType(
                className,
                TypeAttributes.NestedPublic);

            foreach (var vd in cd.varDecl())
            {
                var fieldType = MapType(vd.type());
                foreach (var id in vd.ident())
                {
                    var fb = nested.DefineField(
                        id.GetText(),
                        fieldType,
                        FieldAttributes.Public);
                    _fieldBuilders[(className, id.GetText())] = fb;
                }
            }

            var ctorBuilder = nested.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);
            var ilc = ctorBuilder.GetILGenerator();
            ilc.Emit(OpCodes.Ldarg_0);
            ilc.Emit(OpCodes.Call,
                typeof(object).GetConstructor(Type.EmptyTypes)!);
            ilc.Emit(OpCodes.Ret);

            _ctorBuilders[className] = ctorBuilder;
            _nestedTypes[className] = nested;
        }

        foreach (var nested in _nestedTypes.Values)
        {
            nested.CreateType();
        }

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

    public override object VisitForStmt(MiniCSParser.ForStmtContext ctx)
    {
        if (ctx.forInit() != null)
            Visit(ctx.forInit());

        var loopStart = _il.DefineLabel();
        var condCheck = _il.DefineLabel();
        var loopEnd = _il.DefineLabel();

        _il.Emit(OpCodes.Br, condCheck);

        _il.MarkLabel(loopStart);
        Visit(ctx.statement());

        if (ctx.forUpdate() != null)
            Visit(ctx.forUpdate());

        _il.MarkLabel(condCheck);
        if (ctx.condition() != null)
        {
            Visit(ctx.condition());
            _il.Emit(OpCodes.Brfalse, loopEnd);
        }

        _il.Emit(OpCodes.Br, loopStart);

        _il.MarkLabel(loopEnd);
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
        var des = ctx.designator();

        if (des.DOT().Length > 0)
        {
            var objName = des.ident(0).GetText();
            var lbObj = _locals[objName];
            _il.Emit(OpCodes.Ldloc, lbObj.LocalIndex);
            Visit(ctx.expr());

            var fieldName = des.ident(1).GetText();
            var classTag = _symbols.Lookup(objName)!.TypeTag;
            var className = TypeTag.ClassNameFromTag(classTag)!;
            var fb = _fieldBuilders[(className, fieldName)];
            _il.Emit(OpCodes.Stfld, fb);

            return null;
        }

        if (des.SBL().Length > 0)
        {
            var arrName = des.ident(0).GetText();
            var arrLb = _locals[arrName];
            _il.Emit(OpCodes.Ldloc, arrLb.LocalIndex);

            Visit(des.expr(0));

            Visit(ctx.expr());

            _il.Emit(OpCodes.Stelem_I4);
            return null;
        }

        Visit(ctx.expr());
        var simpleName = des.ident(0).GetText();
        var lbVar = _locals[simpleName];
        _il.Emit(OpCodes.Stloc, lbVar.LocalIndex);
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

    public override object VisitTerm(MiniCSParser.TermContext ctx)
    {
        Visit(ctx.factor(0));

        for (int i = 1; i < ctx.factor().Length; i++)
        {
            Visit(ctx.factor(i));
            var opToken = ctx.mulop(i - 1).GetText();
            switch (opToken)
            {
                case "*":
                    _il.Emit(OpCodes.Mul);
                    break;
                case "/":
                    _il.Emit(OpCodes.Div);
                    break;
                case "%":
                    _il.Emit(OpCodes.Rem);
                    break;
                default:
                    throw new InvalidOperationException($"Operador inesperado en Term: {opToken}");
            }
        }

        return null;
    }


    public override object VisitFactor(MiniCSParser.FactorContext ctx)
    {
        if (ctx.designator() != null && ctx.LEFTP() != null)
        {
            var name = ctx.designator().GetText();
            var args = ctx.actPars()?.expr() ?? Array.Empty<MiniCSParser.ExprContext>();

            switch (name)
            {
                case "len":
                {
                    Visit(args[0]);
                    var listType = VisitAndGetListType(args[0]);
                    var countProp = listType.GetProperty("Count")!;
                    _il.EmitCall(OpCodes.Callvirt, countProp.GetGetMethod()!, null);
                    return null;
                }
                case "add":
                {
                    Visit(args[0]);
                    Visit(args[1]);
                    var addMethod = VisitAndGetListType(args[0]).GetMethod("Add")!;
                    _il.EmitCall(OpCodes.Callvirt, addMethod, null);
                    return null;
                }
                case "del":
                {
                    Visit(args[0]);
                    Visit(args[1]);
                    var removeAt = VisitAndGetListType(args[0]).GetMethod("RemoveAt")!;
                    _il.EmitCall(OpCodes.Callvirt, removeAt, null);
                    return null;
                }
            }
        }

        if (ctx.NEW() != null && ctx.SBL().Length == 0)
        {
            var className = ctx.ident().GetText();
            var ctorBuilder = _ctorBuilders[className];
            _il.Emit(OpCodes.Newobj, ctorBuilder);
            return null;
        }

        if (ctx.NEW() != null && ctx.SBL().Length > 0)
        {
            var elemTypeName = ctx.ident().GetText();
            var elemType = elemTypeName switch
            {
                "int" => typeof(int),
                "char" => typeof(char),
                "bool" => typeof(bool),
                "float" => typeof(float),
                "double" => typeof(double),
                _ => throw new NotSupportedException($"Tipo de lista no soportado: {elemTypeName}")
            };
            var listType = typeof(List<>).MakeGenericType(elemType);
            var ctor = listType.GetConstructor(Type.EmptyTypes)!;
            _il.Emit(OpCodes.Newobj, ctor);
            return null;
        }


        if (ctx.designator() != null && ctx.LEFTP() == null)
        {
            var des = ctx.designator();
            var baseName = des.ident(0).GetText();

            var lb = _locals[baseName];
            _il.Emit(OpCodes.Ldloc, lb.LocalIndex);


            if (des.DOT().Length > 0)
            {
                var fieldName = des.ident(1).GetText();
                var sym = _symbols.Lookup(baseName)!;
                var className = TypeTag.ClassNameFromTag(sym.TypeTag)!;
                var fb = _fieldBuilders[(className, fieldName)];
                _il.Emit(OpCodes.Ldfld, fb);
            }
            else if (des.SBL().Length > 0)
            {
                Visit(des.expr(0));
                _il.Emit(OpCodes.Ldelem_I4);
            }

            return null;
        }


        if (ctx.NUMLIT() != null)
        {
            var v = int.Parse(ctx.NUMLIT().GetText());
            _il.Emit(OpCodes.Ldc_I4, v);
            return null;
        }

        if (ctx.LEFTP() != null && ctx.expr().Length > 0)
        {
            Visit(ctx.expr(0));
            return null;
        }

        if (ctx.CHARLIT() != null)
        {
            var raw = ctx.CHARLIT().GetText();
            var unesc = Regex.Unescape(raw.Substring(1, raw.Length - 2));
            char c = unesc[0];
            _il.Emit(OpCodes.Ldc_I4, (int)c);
            _il.Emit(OpCodes.Conv_U2);
            return null;
        }

        if (ctx.STRINGLIT() != null)
        {
            var raw = ctx.STRINGLIT().GetText();
            var s = Regex.Unescape(raw.Substring(1, raw.Length - 2));
            _il.Emit(OpCodes.Ldstr, s);
            return null;
        }

        if (ctx.TRUE() != null)
        {
            _il.Emit(OpCodes.Ldc_I4_1);
            return null;
        }

        if (ctx.FALSE() != null)
        {
            _il.Emit(OpCodes.Ldc_I4_0);
            return null;
        }

        if (ctx.FLOATLIT() != null)
        {
            float f = float.Parse(ctx.FLOATLIT().GetText().TrimEnd('f', 'F'));
            _il.Emit(OpCodes.Ldc_R4, f);
            return null;
        }

        if (ctx.DOUBLELIT() != null)
        {
            double d = double.Parse(ctx.DOUBLELIT().GetText());
            _il.Emit(OpCodes.Ldc_R8, d);
            return null;
        }

        throw new NotSupportedException($"Factor no soportado: {ctx.GetText()}");
    }

    public override object VisitWriteStmt(MiniCSParser.WriteStmtContext ctx)
    {
        Visit(ctx.expr());

        int tag = _exprTypes[ctx.expr()];

        MethodInfo mi = tag switch
        {
            TypeTag.String => typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })!,
            TypeTag.Char => typeof(Console).GetMethod("WriteLine", new[] { typeof(char) })!,
            TypeTag.Bool => typeof(Console).GetMethod("WriteLine", new[] { typeof(bool) })!,
            TypeTag.Float => typeof(Console).GetMethod("WriteLine", new[] { typeof(float) })!,
            TypeTag.Double => typeof(Console).GetMethod("WriteLine", new[] { typeof(double) })!,
            TypeTag.Int => typeof(Console).GetMethod("WriteLine", new[] { typeof(int) })!,
            _ => throw new NotSupportedException(
                $"No hay WriteLine para tipo {TypeTag.PrettyPrint(tag)}"
            )
        };

        _il.EmitCall(OpCodes.Call, mi, null);

        return null;
    }


    public override object VisitReadStmt(MiniCSParser.ReadStmtContext ctx)
    {
        _il.EmitCall(OpCodes.Call, typeof(Console).GetMethod("ReadLine", Type.EmptyTypes)!, null);
        var name = ctx.designator().GetText();
        var tag = _symbols.Lookup(name)!.TypeTag;

        switch (tag)
        {
            case TypeTag.Int:
                _il.EmitCall(OpCodes.Call, typeof(int).GetMethod("Parse", [typeof(string)])!, null);
                break;
            case TypeTag.Bool:
                _il.EmitCall(OpCodes.Call, typeof(bool).GetMethod("Parse", [typeof(string)])!, null);
                break;
            case TypeTag.Float:
                _il.EmitCall(OpCodes.Call, typeof(float).GetMethod("Parse", [typeof(string)])!, null);
                break;
            case TypeTag.Double:
                _il.EmitCall(OpCodes.Call, typeof(double).GetMethod("Parse", [typeof(string)])!, null);
                break;
            case TypeTag.String:
                // nada, ya es string
                break;
            case TypeTag.Char:
                _il.EmitCall(OpCodes.Call, typeof(string).GetMethod("get_Chars")!, null);
                break;
            default:
                throw new NotSupportedException($"Read no soportado para tag={tag}");
        }

        var lb = _locals[name];
        _il.Emit(OpCodes.Stloc, lb.LocalIndex);
        return null;
    }

    public override object VisitIncStmt(MiniCSParser.IncStmtContext ctx)
    {
        var name = ctx.designator().GetText();
        if (!_locals.TryGetValue(name, out var lb))
            throw new InvalidOperationException($"Variable '{name}' no declarada.");

        _il.Emit(OpCodes.Ldloc, lb.LocalIndex);
        _il.Emit(OpCodes.Ldc_I4_1);
        _il.Emit(OpCodes.Add);
        _il.Emit(OpCodes.Stloc, lb.LocalIndex);

        return null;
    }

    public override object VisitDecStmt(MiniCSParser.DecStmtContext ctx)
    {
        var name = ctx.designator().GetText();
        if (!_locals.TryGetValue(name, out var lb))
            throw new InvalidOperationException($"Variable '{name}' no declarada.");

        _il.Emit(OpCodes.Ldloc, lb.LocalIndex);
        _il.Emit(OpCodes.Ldc_I4_1);
        _il.Emit(OpCodes.Sub);
        _il.Emit(OpCodes.Stloc, lb.LocalIndex);

        return null;
    }


    public override object VisitIfStmt(MiniCSParser.IfStmtContext ctx)
    {
        Visit(ctx.condition());
        var elseL = _il.DefineLabel();
        var endL = _il.DefineLabel();
        _il.Emit(OpCodes.Brfalse, elseL);

        Visit(ctx.statement(0));
        _il.Emit(OpCodes.Br, endL);

        _il.MarkLabel(elseL);
        if (ctx.ELSE() != null)
            Visit(ctx.statement(1));

        _il.MarkLabel(endL);
        return null;
    }

    public override object VisitCondFact(MiniCSParser.CondFactContext ctx)
    {
        Visit(ctx.expr(0));
        Visit(ctx.expr(1));

        switch (ctx.relop().GetText())
        {
            case "==": _il.Emit(OpCodes.Ceq); break;
            case "!=":
                _il.Emit(OpCodes.Ceq);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                break;
            case "<": _il.Emit(OpCodes.Clt); break;
            case ">": _il.Emit(OpCodes.Cgt); break;
            case "<=":
                _il.Emit(OpCodes.Cgt);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                break;
            case ">=":
                _il.Emit(OpCodes.Clt);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                break;
            default: throw new NotSupportedException($"Relop no soportado: {ctx.relop().GetText()}");
        }

        return null;
    }

    public override object VisitCondTerm(MiniCSParser.CondTermContext ctx)
    {
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

        switch (name)
        {
            case "len":
                Visit(args[0]); // apilar la lista
                var listCountProp = VisitAndGetListType(args[0]).GetProperty("Count");
                _il.EmitCall(OpCodes.Callvirt, listCountProp?.GetGetMethod()!, null);
                return null;

            case "add":
                Visit(args[0]);
                Visit(args[1]);
                var listAddMethod = VisitAndGetListType(args[0]).GetMethod("Add");
                if (listAddMethod != null) _il.EmitCall(OpCodes.Callvirt, listAddMethod, null);
                return null;

            case "del":
                Visit(args[0]);
                Visit(args[1]);
                var listType = VisitAndGetListType(args[0]);
                var removeAt = listType.GetMethod("RemoveAt");
                if (removeAt != null) _il.EmitCall(OpCodes.Callvirt, removeAt, null);
                return null;
            default:
                throw new NotSupportedException($"Llamada a '{name}' no implementada.");
        }
    }

    /// <summary>
    /// Helper para obtener el tipo CLR de una lista dada la expresión.
    /// </summary>
    private Type VisitAndGetListType(MiniCSParser.ExprContext expr)
    {
        var tag = _exprTypes[expr];
        // derivar tipo base y dimensiones usando TypeTag utilitario
        var pretty = TypeTag.PrettyPrint(tag);
        // pretty es algo como "int[]" o "int[][]", pero para listas dinámicas usamos List<>
        var (baseName, dims) = TypeTag.ParseFullType(pretty);
        var baseClr = baseName switch
        {
            "int" => typeof(int),
            "char" => typeof(char),
            "bool" => typeof(bool),
            "float" => typeof(float),
            "double" => typeof(double),
            _ => throw new NotSupportedException($"Tipo de lista no soportado: {baseName}")
        };
        // dims indica cuántos niveles de lista
        // sólo permitimos listas unidimensionales
        return typeof(List<>).MakeGenericType(baseClr);
    }

    public override object VisitForInit(MiniCSParser.ForInitContext ctx)
    {
        Visit(ctx.expr());
        var name = ctx.designator().GetText();
        var lb = _locals[name];
        _il.Emit(OpCodes.Stloc, lb.LocalIndex);
        return null;
    }

    public override object VisitForUpdate(MiniCSParser.ForUpdateContext ctx)
    {
        Visit(ctx.expr());
        var name = ctx.designator().GetText();
        var lb = _locals[name];
        _il.Emit(OpCodes.Stloc, lb.LocalIndex);
        return null;
    }

    private Type MapType(MiniCSParser.TypeContext t)
    {
        var baseName = t.ident().GetText();
        var baseClr = baseName switch
        {
            "int" => typeof(int),
            "char" => typeof(char),
            "bool" => typeof(bool), 
            "float" => typeof(float), 
            "double" => typeof(double), 
            "string" => typeof(string),
            _ when _nestedTypes.ContainsKey(baseName)
                => _nestedTypes[baseName],
            _ => throw new NotSupportedException($"Tipo no soportado: {baseName}")
        };

        var dims = t.SBL().Length;
        return dims > 0
            ?
            typeof(List<>).MakeGenericType(baseClr)
            : baseClr;
    }
}