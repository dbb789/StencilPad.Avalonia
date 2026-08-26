using System;
using System.Globalization;
using Antlr4.Runtime.Tree;

namespace StencilPad.Parsers;

public class ArithmeticExpressionVisitor : ArithmeticBaseVisitor<decimal>
{
    public override decimal VisitCompileUnit(ArithmeticParser.CompileUnitContext context)
    {
        return Visit(context.expression());
    }

    public override decimal VisitParens(ArithmeticParser.ParensContext context)
    {
        return Visit(context.expression());
    }

    public override decimal VisitUnaryOp(ArithmeticParser.UnaryOpContext context)
    {
        var value = Visit(context.expression());
        return context.op.Text switch
        {
            "+" => value,
            "-" => -value,
            _ => throw new ArithmeticParseException($"Unsupported unary operator: {context.op.Text}")
        };
    }

    public override decimal VisitPower(ArithmeticParser.PowerContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));
        
        return (decimal)Math.Pow((double)left, (double)right);
    }

    public override decimal VisitMulDivMod(ArithmeticParser.MulDivModContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));

        return context.op.Text switch
        {
            "*" => left * right,
            "/" => right == 0m ? throw new ArithmeticParseException("Division by zero.") : left / right,
            "%" => right == 0m ? throw new ArithmeticParseException("Modulo by zero.") : left % right,
            _ => throw new ArithmeticParseException($"Unsupported binary operator: {context.op.Text}")
        };
    }

    public override decimal VisitAddSub(ArithmeticParser.AddSubContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));

        return context.op.Text switch
        {
            "+" => left + right,
            "-" => left - right,
            _ => throw new ArithmeticParseException($"Unsupported binary operator: {context.op.Text}")
        };
    }
    
    public override decimal VisitFraction(ArithmeticParser.FractionContext context)
    {
        var value = ParseUint(context.UINT(0));
        var numerator = ParseUint(context.UINT(1));
        var denominator = ParseUint(context.UINT(2));

        return value + (numerator / denominator);
    }

    public override decimal VisitDecimal(ArithmeticParser.DecimalContext context)
    {
        var text = context.GetText();
        
        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new ArithmeticParseException($"Invalid numeric format: '{text}'");
    }
    
    public override decimal VisitPi(ArithmeticParser.PiContext context)
    {
        return 3.1415926535m;
    }

    private decimal ParseUint(ITerminalNode node)
    {
        var text = node.GetText();
        
        if (decimal.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new ArithmeticParseException($"Invalid numeric format: '{text}'");
    }
}
