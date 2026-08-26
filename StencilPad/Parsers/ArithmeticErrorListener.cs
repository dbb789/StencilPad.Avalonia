namespace StencilPad.Parsers;

using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

public class ArithmeticErrorListener : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
{
    private readonly List<string> _errors = new();

    public bool HasErrors => _errors.Count > 0;
    public IReadOnlyList<string> Errors => _errors;

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        _errors.Add($"Syntax error at line {line}:{charPositionInLine} - {msg}");
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        _errors.Add($"Lexer error at line {line}:{charPositionInLine} - {msg}");
    }
}
