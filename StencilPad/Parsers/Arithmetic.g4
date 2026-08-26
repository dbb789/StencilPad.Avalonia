grammar Arithmetic;

compileUnit
    : expression EOF
    ;

expression
    : '(' expression ')'                         # Parens
    | op=('+' | '-') expression                  # UnaryOp
    | <assoc=right> expression op='^' expression # Power
    | expression op=('*' | '/' | '%') expression # MulDivMod
    | expression op=('+' | '-') expression       # AddSub
    | UINT '_' UINT '/' UINT                     # Fraction
    | UINT ('.' UINT)?                           # Decimal
    ;

UINT
    : [0-9]+
    ;

WS
    : [ \t\r\n]+ -> skip
    ;
