lexer grammar MiniCSLexer;

// keywords
CLASS       : 'class';
VOID        : 'void';
IF          : 'if';
ELSE        : 'else';
WHILE       : 'while';
FOR         : 'for';
RETURN      : 'return';
BREAK       : 'break';
READ        : 'read';
WRITE       : 'write';
SWITCH      : 'switch';
USING       : 'using';
DEFAULT     : 'default';
CASE        : 'case';


NEW         : 'new';

TRUE        : 'true';
FALSE       : 'false';



//symbols
BL          : '{';
BR          : '}';
COMMA       : ',';
LEFTP       : '(';
RIGHTP      : ')';
SBL         : '[';
SBR         : ']';
ASSIGN      : '=';
ADD         : '++';
SUB         : '--';
OR          : '||';
AND         : '&&';
BAR         : '-';
SEMICOLON   : ';';
DOT         : '.';
PLUS        : '+';
EQEQ        : '==';
NOTEQ       : '!=';
LESS        : '<';
GREATER     : '>';
LESSEQ      : '<=';
GREATEREQ   : '>=';
MULT        : '*';
DIV         : '/';
MOD         : '%';
DOTS        : ':';



//other tokens
NUMLIT: DIGIT DIGIT*;
FLOATLIT: DIGIT+ '.' DIGIT+;
CHARLIT: '\'' (ESC_SEQ | .) '\'';
STRINGLIT: '"' (ESC_SEQ | ~["\\])* '"';
ID: ('_'|LETTER) (LETTER|DIGIT|'.')*;

fragment ESC_SEQ: '\\' ['bfnrt"\\];
fragment LETTER : 'a'..'z' | 'A'..'Z';
fragment DIGIT : '0'..'9' ;

//skip tokens
LINE_COMMENT:   '//' .*? '\r'? '\n' -> skip ;
WS : [ \t\n\r]+ -> skip ;