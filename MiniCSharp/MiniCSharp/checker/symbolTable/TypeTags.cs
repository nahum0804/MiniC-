namespace MiniCSharp.checker.symbolTable
{
    public static class TypeTags
    {
        public const int Unknown     = 0;
        public const int Int         = 1;
        public const int Char        = 2;
        public const int Bool        = 3;
        public const int Float       = 4;
        public const int String      = 5;
        public const int Void        = -1;
        public const int Class       = -2;

        // listas
        public const int ListInt     = 6;
        public const int ListChar    = 7;
        public const int ListBool    = 8;
        public const int ListFloat   = 9;
        public const int ListString  = 10;

        // arreglos
        public const int ArrayInt     = 11;
        public const int ArrayChar    = 12;
        public const int ArrayBool    = 13;
        public const int ArrayFloat   = 14;
        public const int ArrayString  = 15;

        // para recuperar el tipo de elemento (listas o arreglos)
        public static int ElementType(int tag) => tag switch
        {
            ListInt   or ArrayInt   => Int,
            ListChar  or ArrayChar  => Char,
            ListBool  or ArrayBool  => Bool,
            ListFloat or ArrayFloat => Float,
            ListString or ArrayString => String,
            _         => Unknown
        };

        // fábrica de listas
        public static int ListOf(int subtype) => subtype switch
        {
            Int    => ListInt,
            Char   => ListChar,
            Bool   => ListBool,
            Float  => ListFloat,
            String => ListString,
            _      => Unknown
        };

        // fábrica de arreglos
        public static int ArrayOf(int subtype) => subtype switch
        {
            Int    => ArrayInt,
            Char   => ArrayChar,
            Bool   => ArrayBool,
            Float  => ArrayFloat,
            String => ArrayString,
            _      => Unknown
        };

        // mapea un nombre textual a tag
        public static int FromTypeName(string s) => s switch
        {
            "int"      => Int,
            "char"     => Char,
            "bool"     => Bool,
            "float"    => Float,
            "string"   => String,

            // listas
            "List<int>"    => ListInt,
            "List<char>"   => ListChar,
            "List<bool>"   => ListBool,
            "List<float>"  => ListFloat,
            "List<string>" => ListString,

            // arreglos
            "int[]"    => ArrayInt,
            "char[]"   => ArrayChar,
            "bool[]"   => ArrayBool,
            "float[]"  => ArrayFloat,
            "string[]" => ArrayString,

            _ => Unknown
        };

        public static string Name(int tag) => tag switch
        {
            Int    => "int",
            Char   => "char",
            Bool   => "bool",
            Float  => "float",
            String => "string",
            Void   => "void",
            Class  => "class",

            // listas
            ListInt    => "List<int>",
            ListChar   => "List<char>",
            ListBool   => "List<bool>",
            ListFloat  => "List<float>",
            ListString => "List<string>",

            // arreglos
            ArrayInt    => "int[]",
            ArrayChar   => "char[]",
            ArrayBool   => "bool[]",
            ArrayFloat  => "float[]",
            ArrayString => "string[]",

            _ => "?"
        };

        public static bool IsList(int tag) =>
            tag is ListInt or ListChar or ListBool or ListFloat or ListString;

        public static bool IsArray(int tag) =>
            tag is ArrayInt or ArrayChar or ArrayBool or ArrayFloat or ArrayString;
    }
}
