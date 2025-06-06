parser grammar MiniCSParser;

options {
    tokenVocab = MiniCSLexer;
}


program             : usingDecl* CLASS ident BL ( varDecl | classDecl | methodDecl )* BR
                    ;                    
varDecl             : type ident ( COMMA ident )* SEMICOLON
                    ;
classDecl   	    : CLASS ident BL ( varDecl | methodDecl )* BR 
                    ;
methodDecl  	    : ( type | VOID ) ident LEFTP ( formPars )? RIGHTP block
                    ;

formPars    	    : type ident ( COMMA type ident )*
                    ;
                    
type        	    : simpleType                           # simpletype
                    | LIST LESS simpleType GREATER         # listOfSimple
                    | ident ( SBL SBR )?                   # userTypeOrArray
                    ;
                    
simpleType          : INT                                  # intType
                    | CHAR                                 # charType
                    | BOOL                                 # boolType
                    | STRING_TYPE                          # stringType
                    ;
                    
statement           : designator ( ASSIGN expr | LEFTP ( actPars )? RIGHTP | ADD | SUB ) SEMICOLON
            	    | IF LEFTP condition RIGHTP statement ( ELSE statement )?
            	    | FOR LEFTP expr SEMICOLON ( condition )? SEMICOLON ( statement )? RIGHTP statement
            	    | WHILE LEFTP condition RIGHTP statement
                    | BREAK SEMICOLON
                    | RETURN ( expr )?  SEMICOLON
                    | READ LEFTP designator RIGHTP SEMICOLON
                    | WRITE LEFTP expr ( COMMA NUMLIT )? RIGHTP SEMICOLON
                    | SWITCH LEFTP expr RIGHTP BL  caseBlock* SBL DEFAULT DOTS statement* SBR BR SEMICOLON
                    | block
                    | SEMICOLON
                    ;
block       	    : BL ( varDecl | statement )* BR
                    ;
actPars     	    : expr ( COMMA expr )*
                    ;
condition  	        : condTerm ( OR condTerm )*
                    ;
condTerm    	    : condFact ( AND condFact )*
                    ;
condFact    	    : expr relop expr
                    ;
cast        	    : LEFTP type RIGHTP
                    ;
expr        	    : ( BAR )?  ( cast )? term ( addop term )*
                    ;
term        	    : factor ( mulop factor )*
                    ;
factor     	        : designator ( LEFTP ( actPars )? RIGHTP )?
                    | NUMLIT
                    | CHARLIT
                    | STRINGLIT
                    | TRUE
                    | FALSE
                    | NEW ident
                    | LEFTP expr RIGHTP
                    | listLiteral
                    ;   
                    
listLiteral         : LESS expr ( COMMA expr )* GREATER
                    ;
                    
designator  	    : ident ( DOT ident | SBL expr SBR )*
                    ;
relop       	    : EQEQ | NOTEQ | GREATER | GREATEREQ | LESS | LESSEQ
                    ;
addop       	    : PLUS | BAR
                    ;
mulop       	    : MULT | DIV | MOD  
                    ;
ident               : ID
                    ;
                    
caseBlock           : CASE condition DOTS statement*;

usingDecl           : USING ident ( DOT ident )* SEMICOLON;