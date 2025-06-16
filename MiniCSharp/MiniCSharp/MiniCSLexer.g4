lexer grammar MiniCSLexer;

@members {
    // Contador para niveles de comentarios anidados
    private int _commentLevel = 0;
}

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

BL          : '{';
BR          : '}';
COMMA       : ',';
LEFTP       : '(';
RIGHTP      : ')';
SBL         : '[';
SBR         : ']';
ASSIGN      : '=';
COLON       : ':';
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

FLOATLIT    : DIGIT+ '.' DIGIT+ [fF];
DOUBLELIT   : DIGIT+ '.' DIGIT+;
NUMLIT      : DIGIT+;
CHARLIT     : '\'' (ESC_SEQ | .) '\'';
STRINGLIT   : '"' (ESC_SEQ | ~["\\])* '"';
NULL        : 'null' ;
ID          : ('_'|LETTER) (LETTER|DIGIT)*;

// Comentario de línea
LINE_COMMENT
    : '//' ~[\r\n]* -> skip
    ;

COMMENT_START
    : '/*'
      {
        _commentLevel = 1;
      }
      -> pushMode(COMMENT), channel(HIDDEN)
    ;

WS
    : [ \t\r\n]+ -> skip
    ;

fragment ESC_SEQ
    : '\\' ['bfnrt"\\]
    ;
fragment LETTER
    : [a-zA-Z]
    ;
fragment DIGIT
    : [0-9]
    ;

mode COMMENT;

NESTED_COMMENT_START
    : '/*'
      {
        _commentLevel++;
      }
      -> channel(HIDDEN)
    ;

NESTED_COMMENT_END
    : '*/'
      {
        if (--_commentLevel == 0) PopMode();
      }
      -> channel(HIDDEN)
    ;

COMMENT_CONTENT
    : . -> channel(HIDDEN)
    ;

NEWLINE_IN_COMMENT
    : '\r'? '\n' -> channel(HIDDEN)
    ;
