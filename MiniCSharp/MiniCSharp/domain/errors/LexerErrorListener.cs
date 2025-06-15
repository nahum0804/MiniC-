/*
 * File: LexerErrorListener.cs
 * Description: Listener de errores léxicos para ANTLR que reporta errores de tokenización.
 * Author: [Tu Nombre]
 * Date: 2025-06-14
 */

using Antlr4.Runtime;
using System;
using System.IO;

namespace MiniCSharp.domain.errors
{
    /// <summary>
    /// Escucha y reporta errores léxicos durante la fase de análisis de tokens de ANTLR.
    /// Implementa <see cref="IAntlrErrorListener<int>"/> para manejar errores de tipo int.
    /// </summary>
    public class LexerErrorListener : IAntlrErrorListener<int>
    {
        /// <summary>
        /// Se invoca cuando ocurre un error léxico durante la tokenización.
        /// </summary>
        /// <param name="output">Flujo de salida para mensajes de error (no utilizado, imprime en consola).</param>
        /// <param name="recognizer">Instancia del reconocedor de tokens de ANTLR.</param>
        /// <param name="offendingSymbol">Símbolo que provocó el error.</param>
        /// <param name="line">Número de línea donde se detectó el error.</param>
        /// <param name="charPositionInLine">Posición de carácter dentro de la línea.</param>
        /// <param name="msg">Mensaje descriptivo del error.</param>
        /// <param name="e">Excepción de reconocimiento (puede ser null).</param>
        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
            int charPositionInLine,
            string msg, RecognitionException e)
        {
            // Resaltar el error en rojo en la consola
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"Lexical error at line {line}, column {charPositionInLine}: unexpected token {offendingSymbol}. Message: {msg}");
            Console.ResetColor();
        }
    }
}