namespace MiniCSharp.checker
{
    public static class TypeChecker
    {
        public static int Compatible(int t1, int t2, string op)
        {
           
            if (op == "+")
            {
                if (t1 == TypeTags.Int    && t2 == TypeTags.Int)    return TypeTags.Int;
                if (t1 == TypeTags.Double && t2 == TypeTags.Double) return TypeTags.Double;
                if (t1 == TypeTags.String && t2 == TypeTags.String) return TypeTags.String;
            }
            if (op == "-" || op == "*")
            {
                if (t1 == TypeTags.Int    && t2 == TypeTags.Int)    return TypeTags.Int;
                if (t1 == TypeTags.Double && t2 == TypeTags.Double) return TypeTags.Double;
            }
            if (op == "/")
            {
                if (t1 == TypeTags.Int    && t2 == TypeTags.Int)    return TypeTags.Double;
                if (t1 == TypeTags.Double && t2 == TypeTags.Double) return TypeTags.Double;
            }
            if (op == "%")
            {
                if (t1 == TypeTags.Int && t2 == TypeTags.Int) return TypeTags.Int;
            }
            if (op is "==" or "!=" or "<" or "<=" or ">" or ">=")
            {
                if (t1 == t2 && (t1 == TypeTags.Int || t1 == TypeTags.Double || t1 == TypeTags.Char))
                    return TypeTags.Bool;
            }
            if (op is "&&" or "||")
            {
                if (t1 == TypeTags.Bool && t2 == TypeTags.Bool) return TypeTags.Bool;
            }

            return TypeTags.Unknown;
        }
    }
}