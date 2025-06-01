parser grammar MiniCSParser;

options {
    tokenVocab = MiniCSLexer;
}


program             : CLASS ident BL ( varDecl | classDecl | methodDecl )* BR
                    ;                    
varDecl             : type ident ( COMMA ident )* SEMICOLON
                    ;
classDecl   	    : CLASS ident BL varDecl* BR 
                    ;
methodDecl  	    : ( type | VOID ) ident LEFTP ( formPars )? RIGHTP block
                    ;


formPars    	    : type ident ( COMMA type ident )*
                    ;
type        	    : ident ( SBL SBR )?
                    ;
statement           : designator ( ASSIGN expr | LEFTP ( actPars )? RIGHTP | ADD | SUB ) SEMICOLON
            	    | IF LEFTP condition RIGHTP statement ( ELSE statement )?
            	    | FOR LEFTP expr SEMICOLON ( condition )? SEMICOLON ( statement )? RIGHTP statement
            	    | WHILE LEFTP condition RIGHTP statement
                    | BREAK SEMICOLON
                    | RETURN ( expr )?  SEMICOLON
                    | READ LEFTP designator RIGHTP SEMICOLON
                    | WRITE LEFTP expr ( COMMA NUMLIT )? RIGHTP SEMICOLON
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