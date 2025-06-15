/*
 * File: TypeTag.cs
 * Description: Clase utilitaria que gestiona etiquetas de tipo para MiniCSharp.
 * Author: [Tu Nombre]
 * Date: 2025-06-14
 */

using MiniCSharp.checker;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniCSharp.utils
{
    /// <summary>
    /// Provee constantes y utilidades para manejar etiquetas (tags) de tipos de datos y clases personalizadas.
    /// </summary>
    public static class TypeTag
    {
        /// <summary>No reconocido o error.</summary>
        public const int Unknown = 0;
        /// <summary>Tipo entero.</summary>
        public const int Int = 1;
        /// <summary>Tipo carácter.</summary>
        public const int Char = 2;
        /// <summary>Tipo booleano.</summary>
        public const int Bool = 3;
        /// <summary>Tipo flotante.</summary>
        public const int Float = 4;
        /// <summary>Tipo cadena.</summary>
        public const int String = 5;
        /// <summary>Tipo doble precisión.</summary>
        public const int Double = 6;
        /// <summary>Tipo void (sin retorno).</summary>
        public const int Void = -1;
        /// <summary>Etiqueta base para listas (multiplica dimensiones).</summary>
        public const int ListBase = 100;

        /// <summary>
        /// Obtiene la etiqueta de tipo a partir del nombre simple de tipo.
        /// </summary>
        /// <param name="typeName">Nombre del tipo (p.ej. "int", "string" o clase declarada).</param>
        /// <returns>Etiqueta numérica del tipo, o Unknown si no lo reconoce.</returns>
        public static int FromTypeName(string typeName)
        {
            return typeName switch
            {
                "int" => Int,
                "char" => Char,
                "bool" => Bool,
                "float" => Float,
                "string" => String,
                "double" => Double,
                _ when SymbolTableVisitor.CurrentSymbolTable?.IsClassDeclared(typeName) == true
                    => RegisterClass(typeName),
                _ => Unknown
            };
        }

        /// <summary>
        /// Obtiene la etiqueta de tipo para arreglos a partir de texto con corchetes (p.ej. "int[][]").
        /// </summary>
        /// <param name="typeText">Texto del tipo incluyendo dimensiones.</param>
        /// <returns>Etiqueta con dimensiones codificadas, o Unknown si el tipo base no existe.</returns>
        public static int FromTypeNameWithBrackets(string typeText)
        {
            var (baseName, dims) = ParseFullType(typeText);
            var baseTag = FromTypeName(baseName);
            if (baseTag == Unknown) return Unknown;
            return ListBase * dims + baseTag;
        }

        /// <summary>
        /// Parsea un tipo completo con corchetes y devuelve nombre base y dimensiones.
        /// </summary>
        /// <param name="fullType">Tipo textual (p.ej. "MyClass[]" o "int[][]").</param>
        /// <returns>Tupla con nombre base y número de dimensiones.</returns>
        public static (string baseType, int dimensions) ParseFullType(string fullType)
        {
            var name = fullType;
            var dims = 0;
            while (name.EndsWith("[]"))
            {
                dims++;
                name = name[..^2];
            }
            return (name, dims);
        }

        /// <summary>
        /// Convierte una etiqueta de tipo en su representación legible (p.ej. "int[][]" o nombre de clase).
        /// </summary>
        /// <param name="tag">Etiqueta numérica del tipo.</param>
        /// <returns>Cadena con el nombre del tipo.</returns>
        public static string PrettyPrint(int tag)
        {
            if (tag < ListBase)
            {
                return tag switch
                {
                    Void => "void",
                    Int => "int",
                    Char => "char",
                    Bool => "bool",
                    Float => "float",
                    String => "string",
                    Double => "double",
                    _ => "unknown"
                };
            }
            var baseTag = tag % ListBase;
            var dim = tag / ListBase;
            if (IsCustomClass(tag))
                return ClassNameFromTag(tag)!;
            return PrettyPrint(baseTag) + string.Concat(Enumerable.Repeat("[]", dim));
        }

        private static int _nextClassId = 200;
        private static readonly Dictionary<string, int> CustomClassTags = new();
        private static readonly Dictionary<int, string> TagToClassName = new();

        /// <summary>
        /// Registra una nueva clase personalizada y devuelve su etiqueta única.
        /// </summary>
        /// <param name="className">Nombre de la clase a registrar.</param>
        /// <returns>Etiqueta numérica asignada.</returns>
        public static int RegisterClass(string className)
        {
            if (CustomClassTags.TryGetValue(className, out var value))
                return value;
            var newTag = _nextClassId++;
            CustomClassTags[className] = newTag;
            TagToClassName[newTag] = className;
            return newTag;
        }

        /// <summary>
        /// Indica si una etiqueta corresponde a una clase personalizada registrada.
        /// </summary>
        /// <param name="tag">Etiqueta a verificar.</param>
        /// <returns>True si corresponde a clase personalizada.</returns>
        public static bool IsCustomClass(int tag) => TagToClassName.ContainsKey(tag);

        /// <summary>
        /// Obtiene el nombre de clase asociado a una etiqueta personalizada.
        /// </summary>
        /// <param name="tag">Etiqueta de clase.</param>
        /// <returns>Nombre de la clase o null si no existe.</returns>
        public static string? ClassNameFromTag(int tag) => TagToClassName.GetValueOrDefault(tag);
     
        public static bool IsNumeric(int tag)
        {
            return tag == Int
                   || tag == Float
                   || tag == Double;
        }

        public static bool IsBoolean(int tag)
        {
            return tag == Bool;
        }

        
    }
}
