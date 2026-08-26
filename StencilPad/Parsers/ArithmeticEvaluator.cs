using Antlr4.Runtime;
using StencilPad.Spatial;

namespace StencilPad.Parsers;

public static class ArithmeticEvaluator
{
    public static Unit? TryEvaluate(string expression, UnitType unitType)
    {
        try
        {
            return Evaluate(expression, unitType);
        }
        catch (ArithmeticParseException)
        {
            return null;
        }
    }
    
    public static Unit? Evaluate(string expression, UnitType unitType)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var errorListener = new ArithmeticErrorListener();

        var inputStream = new AntlrInputStream(expression);
        var lexer = new ArithmeticLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errorListener);

        var tokenStream = new CommonTokenStream(lexer);
        var parser = new ArithmeticParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);

        var compileUnit = parser.compileUnit();

        if (errorListener.HasErrors)
        {
            throw new ArithmeticParseException(string.Join("; ", errorListener.Errors));
        }

        var visitor = new ArithmeticExpressionVisitor();
        
        return Unit.FromType(visitor.Visit(compileUnit), unitType);
    }
}
