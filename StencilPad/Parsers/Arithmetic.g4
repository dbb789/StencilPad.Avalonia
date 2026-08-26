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
    | NUMBER                                     # Number
    ;

NUMBER
    : [0-9]+ ('.' [0-9]+)?
    | '.' [0-9]+
    ;

WS
    : [ \t\r\n]+ -> skip
    ;
