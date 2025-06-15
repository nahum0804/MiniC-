lexer grammar MiniCSLexer;

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

NEW         : 'new';

TRUE        : 'true';
FALSE       : 'false';

SWITCH      : 'switch';
CASE        : 'case';
DEFAULT     : 'default';

// symbols
BL          : '{';
BR          : '}';
COMMA       : ',';
LEFTP       : '(';
RIGHTP      : ')';
SBL         : '[';
SBR         : ']';
ASSIGN      : '=';
COLON       : ':';   // añadido
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
GREATEREQ   : '>=';
LESSEQ      : '<=';
MULT        : '*';
DIV         : '/';
MOD         : '%';

//other tokens
FLOATLIT : DIGIT+ '.' DIGIT+ [fF];
DOUBLELIT : DIGIT+ '.' DIGIT+;
NUMLIT: DIGIT+;
CHARLIT: '\'' (ESC_SEQ | .) '\'';
STRINGLIT: '"' (ESC_SEQ | ~["\\])* '"';
NULL : 'null' ;
ID: ('_'|LETTER) (LETTER|DIGIT)*;

fragment ESC_SEQ: '\\' ['bfnrt"\\];
fragment LETTER : 'a'..'z' | 'A'..'Z';
fragment DIGIT : '0'..'9' ;

//skip tokens
LINE_COMMENT:   '//' .*? '\r'? '\n' -> skip ;
WS : [ \t\n\r]+ -> skip ;