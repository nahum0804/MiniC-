using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using generated.parser;

namespace MiniCSharp.checker.symbolTable
{
    public class SymbolTableVisitor : MiniCSParserBaseVisitor<object>
    {
        public SymbolTable Table { get; } = new SymbolTable();

        private void ReportError(string msg, IToken tok)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error semántico: {msg} (línea {tok.Line}, col {tok.Column})");
            Console.ResetColor();
        }

        private int GetTypeTag(string typeName, IToken tok)
        {
            var tag = TypeTags.FromTypeName(typeName);
            if (tag == TypeTags.Unknown)
                ReportError($"Tipo '{typeName}' desconocido", tok);
            return tag;
        }

        // 1) Programa: abre scope global, registra la clase principal y recorre sus miembros
        public override object VisitProgram(MiniCSParser.ProgramContext ctx)
        {
            // 1) Ámbito global
            Table.OpenScope();

            // 2) Ámbito de la clase
            Table.OpenScope();

            // 3) Inserta la clase principal
            var classTok = ctx.ident().Start;
            if (!Table.InsertClass(classTok, ctx))
                ReportError($"Clase '{classTok.Text}' redeclarada", classTok);

            // 4) Campos y miembros
            foreach (var vd in ctx.varDecl())    VisitVarDecl(vd);
            foreach (var cd in ctx.classDecl())  VisitClassDecl(cd);
            foreach (var md in ctx.methodDecl()) VisitMethodDecl(md);

            // <- NO cerrar aquí los scopes de clase ni global

            return null;
        }



        // 2) Variables (campos de clase o locales)
        public override object VisitVarDecl([NotNull] MiniCSParser.VarDeclContext ctx)
        {
            var typeTok = ctx.type().Start;
            var tag = GetTypeTag(ctx.type().GetText(), typeTok);

            foreach (var idCtx in ctx.ident())
            {
                var tok = idCtx.Start;
                if (!Table.InsertVariable(tok, tag, isConstant: false, ctx))
                    ReportError($"Variable '{tok.Text}' redeclarada en el mismo scope", tok);
            }
            return null;
        }

        // 3) Clases anidadas (si aplica)
        public override object VisitClassDecl([NotNull] MiniCSParser.ClassDeclContext ctx)
        {
            var tok = ctx.ident().Start;
            var name = tok.Text;
            if (!Table.InsertClass(tok, ctx))
                ReportError($"Clase '{name}' redeclarada", tok);

            Table.OpenScope();
            foreach (var f in ctx.varDecl())    VisitVarDecl(f);
            foreach (var m in ctx.methodDecl()) VisitMethodDecl(m);
            Table.CloseScope();
            return null;
        }

        // 4) Métodos: firma + parámetros + cuerpo
        public override object VisitMethodDecl([NotNull] MiniCSParser.MethodDeclContext ctx)
        {
            var tok = ctx.ident().Start;
            var methodName = tok.Text;

            // Tipo de retorno
            var returnTag = ctx.VOID() != null
                ? TypeTags.Void
                : GetTypeTag(ctx.type().GetText(), ctx.type().Start);

            // Lista de tipos de parámetros
            var paramTags = new List<int>();
            if (ctx.formPars() != null)
            {
                for (int i = 0; i < ctx.formPars().ident().Length; i++)
                {
                    var pTypeCtx = ctx.formPars().type(i);
                    paramTags.Add(GetTypeTag(pTypeCtx.GetText(), pTypeCtx.Start));
                }
            }

            // Inserta el método
            if (!Table.InsertMethod(tok, returnTag, paramTags, ctx))
                ReportError($"Método '{methodName}' redeclarado", tok);


            // Parámetros
            if (ctx.formPars() != null)
            {
                for (int i = 0; i < ctx.formPars().ident().Length; i++)
                {
                    var idTok = ctx.formPars().ident(i).Start;
                    var tag   = paramTags[i];
                    if (!Table.InsertVariable(idTok, tag, isConstant: false, ctx.formPars()))
                        ReportError($"Parámetro '{idTok.Text}' duplicado", idTok);
                }
            }

            Visit(ctx.block());
            return null;
        }

        // 5) Bloques: abrir/cerrar scope e insertar variables locales
        public override object VisitBlock([NotNull] MiniCSParser.BlockContext ctx)
        {

            // Variables locales
            foreach (var v in ctx.varDecl())
                VisitVarDecl(v);

            // Resto de statements
            foreach (var st in ctx.statement())
                Visit(st);

            return null;
        }
    }
}
