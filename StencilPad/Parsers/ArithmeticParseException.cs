using System;

namespace StencilPad.Parsers;

public class ArithmeticParseException : Exception
{    
    public int Line { get; }
    public int Column { get; }

    public ArithmeticParseException(string message)
        : base(message)
    {
        // ...
    }

    public ArithmeticParseException(string message, Exception inner)
        : base(message, inner)
    {
        // ...
    }

    public ArithmeticParseException(string message, int line, int column) 
        : base($"{message} (Line {line}, Column {column})")
    {
        Line = line;
        Column = column;
    }
}
