/*
 * File: Symbol.cs
 * Description: Clase base abstracta para todos los símbolos (variables, métodos, clases, etc.)
 *              en la tabla de símbolos del compilador MiniCSharp.
 * Author: [Tu Nombre]
 * Date: 2025-06-14
 */

using Antlr4.Runtime;

namespace MiniCSharp.checker
{
    /// <summary>
    /// Representa un símbolo genérico en la tabla de símbolos.
    /// Contiene información común a todos los símbolos, como token, tipo, nivel de alcance y contexto de declaración.
    /// </summary>
    public abstract class Symbol
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="Symbol"/>.
        /// </summary>
        /// <param name="token">
        ///   Token ANTLR donde se declara el símbolo, utilizado para reportes de errores.
        /// </param>
        /// <param name="typeTag">
        ///   Código entero que identifica el tipo del símbolo (véase <c>TypeTag</c>).
        /// </param>
        /// <param name="scopeLevel">
        ///   Nivel de alcance (scope) donde se declara el símbolo.
        /// </param>
        /// <param name="declContext">
        ///   Contexto del parser para la regla de declaración asociada.
        /// </param>
        protected Symbol(IToken token, int typeTag, int scopeLevel, ParserRuleContext declContext)
        {
            Token = token;
            TypeTag = typeTag;
            ScopeLevel = scopeLevel;
            DeclContext = declContext;
        }

        /// <summary>
        /// Obtiene el token ANTLR en el que se declaró el símbolo.
        /// </summary>
        public IToken Token { get; }

        /// <summary>
        /// Obtiene el código entero que identifica el tipo del símbolo.
        /// </summary>
        public int TypeTag { get; }

        /// <summary>
        /// Obtiene el nivel de alcance (scope) donde se declaró el símbolo.
        /// </summary>
        public int ScopeLevel { get; }

        /// <summary>
        /// Obtiene el contexto de ANTLR de la declaración del símbolo.
        /// </summary>
        public ParserRuleContext DeclContext { get; }
    }
}
