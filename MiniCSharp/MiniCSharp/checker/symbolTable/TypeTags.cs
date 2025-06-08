namespace MiniCSharp.checker
{
    public static class TypeTags
    {
        public const int Unknown    =  0;
        public const int Int        =  1;
        public const int Char       =  2;
        public const int Bool       =  3;
        public const int Double     =  4;
        public const int String     =  5;
        public const int Void       = -1;
        public const int Class      = -2;

        // Nuevos tags para listas
        public const int ListInt     =  6;
        public const int ListChar    =  7;
        public const int ListBool    =  8;
        public const int ListDouble  =  9;
        public const int ListString  = 10;

        public static int FromTypeName(string s) => s switch
        {
            "int"         => Int,
            "char"        => Char,
            "bool"        => Bool,
            "double"      => Double,
            "string"      => String,
            "List<int>"   => ListInt,
            "List<char>"  => ListChar,
            "List<bool>"  => ListBool,
            "List<double>"=> ListDouble,
            "List<string>"=> ListString,
            _             => Unknown
        };
        
        public static int ListOf(int subtype) => subtype switch
        {
            Int    => ListInt,
            Char   => ListChar,
            Bool   => ListBool,
            Double => ListDouble,
            String => ListString,
            _      => Unknown
        };
        
        public static string Name(int tag) => tag switch
        {
            Int         => "int",
            Char        => "char",
            Bool        => "bool",
            Double      => "double",
            String      => "string",
            Void        => "void",
            Class       => "class",
            ListInt     => "List<int>",
            ListChar    => "List<char>",
            ListBool    => "List<bool>",
            ListDouble  => "List<double>",
            ListString  => "List<string>",
            _           => "?"
        };
    }
}
