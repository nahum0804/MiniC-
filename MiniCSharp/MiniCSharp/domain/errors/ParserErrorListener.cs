using Antlr4.Runtime;

namespace MiniCSharp.domain.errors;

public class ParserErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken? offendingSymbol, int line,
        int charPositionInLine,
        string msg, RecognitionException e)
    {
        Console.ForegroundColor = ConsoleColor.Red;

        var errorText = offendingSymbol?.Text ?? "<EOF>";

        var posExpecting = msg.IndexOf("expecting", StringComparison.Ordinal);
        var detail = posExpecting >= 0
            ? msg[..posExpecting].Trim()
            : msg;

        Console.WriteLine($"Syntax error: Line: {line}, Column: {charPositionInLine}: “{errorText}” → {detail}");
        Console.ResetColor();
    }
}