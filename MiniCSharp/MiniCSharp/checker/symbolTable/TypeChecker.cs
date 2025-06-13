namespace MiniCSharp.checker.symbolTable;

public static class TypeChecker
{
    public static int Compatible(int t1, int t2, string op)
    {
        // Si alguno es lista, sólo == y != son válidos
        if ((TypeTags.IsList(t1) || TypeTags.IsList(t2))
            && op is not "==" and not "!=")
            return TypeTags.Unknown;

        switch (op)
        {
            case "+" when t1 == TypeTags.Int   && t2 == TypeTags.Int:
                return TypeTags.Int;
            case "+" when t1 == TypeTags.Float && t2 == TypeTags.Float:
                return TypeTags.Float;
            case "+" when t1 == TypeTags.String && t2 == TypeTags.String:
                return TypeTags.String;

            case "-" or "*":
                if (t1 == TypeTags.Int   && t2 == TypeTags.Int)   return TypeTags.Int;
                if (t1 == TypeTags.Float && t2 == TypeTags.Float) return TypeTags.Float;
                break;

            case "/" when t1 == TypeTags.Int   && t2 == TypeTags.Int:
            case "/" when t1 == TypeTags.Float && t2 == TypeTags.Float:
                return TypeTags.Float;

            case "%" when t1 == TypeTags.Int && t2 == TypeTags.Int:
                return TypeTags.Int;

            // == y !=: ahora también bool y string
            case "==" or "!="
                when t1 == t2
                     && ( t1 == TypeTags.Int
                          || t1 == TypeTags.Float
                          || t1 == TypeTags.Char
                          || t1 == TypeTags.String
                          || t1 == TypeTags.Bool
                          || TypeTags.IsList(t1) ):
                return TypeTags.Bool;

            // <, >, <=, >= : sólo numéricos y char
            case "<" or "<=" or ">" or ">="
                when t1 == t2
                     && (t1 == TypeTags.Int
                         || t1 == TypeTags.Float
                         || t1 == TypeTags.Char):
                return TypeTags.Bool;

            // &&, ||
            case "&&" or "||" when t1 == TypeTags.Bool && t2 == TypeTags.Bool:
                return TypeTags.Bool;
        }

        return TypeTags.Unknown;
    }
}