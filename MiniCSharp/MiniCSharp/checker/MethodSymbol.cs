/*
 * File: MethodSymbol.cs
 * Description: Define un símbolo de método para el analizador semántico de MiniCSharp,
 *              incluyendo información sobre tipo de retorno y tipos de parámetros.
 * Author: [Tu Nombre]
 * Date: 2025-06-14
 */

using Antlr4.Runtime;

namespace MiniCSharp.checker
{
    /// <summary>
    /// Representa el símbolo de un método en la fase de chequeo semántico.
    /// Almacena la información de retorno y los tipos de sus parámetros.
    /// </summary>
    /// <param name="token">
    ///   Token de ANTLR donde se declara el método (para informes de error y posición).
    /// </param>
    /// <param name="returnTypeTag">
    ///   Código entero que identifica el tipo de retorno del método.
    /// </param>
    /// <param name="scopeLevel">
    ///   Nivel de alcance (scope) donde está declarado el método.
    /// </param>
    /// <param name="declContext">
    ///   Contexto del parser para la regla de declaración de método.
    /// </param>
    /// <param name="paramTypeTags">
    ///   Lista de códigos enteros que identifican los tipos de cada parámetro.
    /// </param>
    public class MethodSymbol(
        IToken token,
        int returnTypeTag,
        int scopeLevel,
        ParserRuleContext declContext,
        List<int> paramTypeTags
    )
        : Symbol(token, returnTypeTag, scopeLevel, declContext)
    {
        /// <summary>
        /// Código entero que identifica el tipo de retorno del método.
        /// </summary>
        public int ReturnTypeTag { get; } = returnTypeTag;

        /// <summary>
        /// Lista de códigos de tipo para cada parámetro del método.
        /// </summary>
        public List<int> ParamTypeTags { get; } = paramTypeTags;

    }
}