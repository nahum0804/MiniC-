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
LIST        : 'List';

NEW         : 'new';
TRUE        : 'true';
FALSE       : 'false';

INT         : 'int';
CHAR        : 'char';
BOOL        : 'bool';
STRING_TYPE : 'string';
FLOAT       : 'float';

// symbols
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
COLON       : ':';

// literals
FLOATLIT    : DIGIT+ '.' DIGIT+ [fF]? ;
NUMLIT      : DIGIT+ ;
CHARLIT     : '\'' (ESC_SEQ | ~['\\]) '\'' ;
STRINGLIT   : '"' (ESC_SEQ | ~["\\])* '"' ;
ID          : ('_' | LETTER) (LETTER | DIGIT | '.')* ;

// whitespace & comments
WS  : [ \t\r\n]+ -> skip ;
LINE_COMMENT
    : '//' ~[\r\n]* -> skip
    ;
BLOCK_COMMENT_START
    : '/*' -> pushMode(COMMENT_MODE), skip
    ;
mode COMMENT_MODE;
    NESTED_COMMENT_START
        : '/*' -> pushMode(COMMENT_MODE)
        ;
    COMMENT_END
        : '*/' -> popMode, skip
        ;

    COMMENT_WS
        : [\t\r\n]+ -> skip
        ;
   
    COMMENT_CHAR
        : . -> skip
        ;

// fragments
fragment ESC_SEQ  : '\\' ['bfnrt"\\] ;
fragment LETTER   : [a-zA-Z] ;
fragment DIGIT    : [0-9] ;
