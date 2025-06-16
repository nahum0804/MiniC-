/*
 * File: ParserErrorListener.cs
 * Description: Listener de errores sintácticos para ANTLR que formatea y reporta errores de parsing.
 * Author: [Tu Nombre]
 * Date: 2025-06-14
 */

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.IO;

namespace MiniCSharp.domain.errors
{
    /// <summary>
    /// Escucha y reporta errores sintácticos durante la fase de parsing de ANTLR.
    /// Hereda de <see cref="BaseErrorListener"/> y formatea el mensaje para consola.
    /// </summary>
    public class ParserErrorListener : BaseErrorListener
    {
        /// <summary>
        /// Se invoca cuando ocurre un error sintáctico durante el parseo.
        /// </summary>
        /// <param name="output">Flujo de salida para mensajes (no usado aquí, se imprime en consola).</param>
        /// <param name="recognizer">Instancia del parser de ANTLR.</param>
        /// <param name="offendingSymbol">Token que causó el error, o null si es EOF.</param>
        /// <param name="line">Número de línea donde se detectó el error.</param>
        /// <param name="charPositionInLine">Posición de carácter dentro de la línea.</param>
        /// <param name="msg">Mensaje original de ANTLR describiendo el error.</param>
        /// <param name="e">Excepción de reconocimiento (puede ser null).</param>
        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken? offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            // Cambiar color de texto a rojo para resaltar el error
            Console.ForegroundColor = ConsoleColor.Red;

            // Obtener el texto del token que produjo el error (o <EOF> si es null)
            var errorText = offendingSymbol?.Text ?? "<EOF>";
            // ① Obtiene los tokens válidos en ese punto
            var parser = (Parser)recognizer;
            var expected = parser.GetExpectedTokens()
                .ToArray(); // ej. {59,60}
            var expectedNames = string.Join(", ",
                expected.Select(t => parser.Vocabulary.GetDisplayName(t)));

            // Mensaje claro
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Syntax error (line {line}, col {charPositionInLine + 1}): " +
                              $"expected {expectedNames} but found '{errorText}'.");
            Console.ResetColor();
        }
    }
}