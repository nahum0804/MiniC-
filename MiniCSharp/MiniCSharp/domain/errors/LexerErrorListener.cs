using Antlr4.Runtime;

namespace MiniCSharp.domain.errors;

public class LexerErrorListener : IAntlrErrorListener<int>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
        int charPositionInLine,
        string msg, RecognitionException e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(
            $"Lexical error in the line: {line}, Column: {charPositionInLine}: unexpected error: {msg}");
        Console.ResetColor();
    }
}