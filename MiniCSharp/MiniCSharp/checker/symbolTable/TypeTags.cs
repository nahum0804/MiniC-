namespace MiniCSharp.checker.symbolTable
{
    public static class TypeTags
    {
        public const int Unknown = 0;
        public const int Int = 1;
        public const int Char = 2;
        public const int Bool = 3;
        public const int Float = 4;
        public const int String = 5;
        public const int Void = -1;
        public const int Class = -2;

        public const int ListInt = 6;
        public const int ListChar = 7;
        public const int ListBool = 8;
        public const int ListFloat = 9;
        public const int ListString = 10;

        public static int ElementType(int tag) => tag switch
        {
            ListInt => Int,
            ListChar => Char,
            ListBool => Bool,
            ListFloat => Float,
            ListString => String,
            _ => Unknown
        };

        public static int FromTypeName(string s) => s switch
        {
            "int" => Int,
            "char" => Char,
            "bool" => Bool,
            "float" => Float,
            "string" => String,
            "List<int>" => ListInt,
            "List<char>" => ListChar,
            "List<bool>" => ListBool,
            "List<float>" => ListFloat,
            "List<string>" => ListString,
            _ => Unknown
        };

        public static int ListOf(int subtype) => subtype switch
        {
            Int => ListInt,
            Char => ListChar,
            Bool => ListBool,
            Float => ListFloat,
            String => ListString,
            _ => Unknown
        };

        public static string Name(int tag) => tag switch
        {
            Int => "int",
            Char => "char",
            Bool => "bool",
            Float => "float",
            String => "string",
            Void => "void",
            Class => "class",
            ListInt => "List<int>",
            ListChar => "List<char>",
            ListBool => "List<bool>",
            ListFloat => "List<float>",
            ListString => "List<string>",
            _ => "?"
        };

        public static bool IsList(int tag) =>
            tag is ListInt or ListChar or ListBool or ListFloat or ListString;
    }
}