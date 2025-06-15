/*
 * File: VariableSymbol.cs
 * Description: Define un símbolo de variable para la tabla de símbolos de MiniCSharp.
 * Author: [Tu Nombre]
 * Date: 2025-06-14
 */

using System.Reflection.Emit;
using Antlr4.Runtime;

namespace MiniCSharp.checker
{
    /// <summary>
    /// Representa un símbolo de variable en el chequeo semántico.
    /// Almacena información sobre el tipo, nivel de alcance y si la variable es constante.
    /// </summary>
    /// <param name="token">
    ///   Token ANTLR donde se declara la variable (para posicionar errores).
    /// </param>
    /// <param name="typeTag">
    ///   Código entero que identifica el tipo de la variable.
    /// </param>
    /// <param name="scopeLevel">
    ///   Nivel de alcance (scope) donde se declara la variable.
    /// </param>
    /// <param name="declContext">
    ///   Contexto del parser de la declaración de la variable.
    /// </param>
    /// <param name="isConstant">
    ///   Indica si la variable es constante (solo lectura).
    /// </param>
    public class VariableSymbol(
        IToken token,
        int typeTag,
        int scopeLevel,
        ParserRuleContext declContext,
        bool isConstant
    ) : Symbol(token, typeTag, scopeLevel, declContext)
    {
        
        public LocalBuilder? LocalBuilder { get; set; }

        /// <summary>
        /// Indica si la variable se declaró como constante (readonly).
        /// </summary>
        public bool IsConstant { get; } = isConstant;

    }
}