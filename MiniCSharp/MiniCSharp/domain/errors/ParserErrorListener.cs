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
            IToken offendingSymbol,
            int line,
            int charPos,
            string msg,
            RecognitionException e)
        {
            var parser = (Parser)recognizer;
            var expected = parser.GetExpectedTokens().ToArray();
            var expectedNames = expected
                .Select(t => parser.Vocabulary.GetDisplayName(t))
                .Distinct()
                .ToArray();

            string found = offendingSymbol?.Text ?? "<EOF>";
            string finalMessage;

            // --- Regla 1: llegó EOF pero faltaba una llave ---
            if (found == "<EOF>" && expectedNames.Contains("'}'"))
            {
                finalMessage = "missing '}' before end of file";
            }
            // --- Regla 2: aparece '}' sin que se esperara ---
            else if (found == "}" && !expectedNames.Contains("'}'"))
            {
                finalMessage = "unexpected '}', no matching '{'";
            }
            // --- Caso general ---
            else
            {
                string exp =
                    expectedNames.Length == 1
                        ? expectedNames[0]
                        : string.Join(" or ", expectedNames);
                finalMessage = $"expected {exp} but found '{found}'";
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Syntax error (line {line}, col {charPos + 1}): {finalMessage}");
            Console.ResetColor();
        }
    }
}