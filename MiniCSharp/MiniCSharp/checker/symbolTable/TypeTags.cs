namespace MiniCSharp.checker
{
    public static class TypeTags
    {
        public const int Unknown =  0;
        public const int Int     =  1;
        public const int Char    =  2;
        public const int Bool    =  3;
        public const int Double  =  4;
        public const int String  =  5;
        public const int Void    = -1;
        public const int Class   = -2;

        public static int FromTypeName(string s) => s switch
        {
            "int"    => Int,
            "char"   => Char,
            "bool"   => Bool,
            "double" => Double,
            "string" => String,
            _        => Unknown
        };
    }
}