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
type        	    : ident ( SBL SBR )* 
                    ;
caseBlock
                    : CASE expr COLON statement*
                    ;
statement
                    : designator ASSIGN expr SEMICOLON                       # assignStmt
                    | designator LEFTP ( actPars )? RIGHTP SEMICOLON         # callStmt
                    | designator ADD SEMICOLON                               # incStmt
                    | designator SUB SEMICOLON                               # decStmt
                    | IF LEFTP condition RIGHTP statement ( ELSE statement )? # ifStmt
                    | FOR LEFTP forInit? SEMICOLON condition? SEMICOLON forUpdate? RIGHTP statement  # forStmt
                    | WHILE LEFTP condition RIGHTP statement                 # whileStmt
                    | BREAK SEMICOLON                                        # breakStmt
                    | RETURN ( expr )? SEMICOLON                             # returnStmt
                    | READ LEFTP designator RIGHTP SEMICOLON                 # readStmt
                    | WRITE LEFTP expr ( COMMA NUMLIT )? RIGHTP SEMICOLON    # writeStmt
                    | SWITCH LEFTP expr RIGHTP BL caseBlock* ( DEFAULT COLON statement* )? BR # switchStmt
                    | block                                                  # blockStmt
                    | SEMICOLON                                              # emptyStmt
                    ;

forInit   : designator ASSIGN expr ;
forUpdate : designator ASSIGN expr ;

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
factor              : designator ( LEFTP ( actPars )? RIGHTP )?
                    | NUMLIT
                    | FLOATLIT                 
                    | DOUBLELIT                
                    | CHARLIT
                    | STRINGLIT
                    | TRUE
                    | FALSE
                    | NEW ident LEFTP RIGHTP         // # newObject
                    | NEW ident ( SBL expr SBR )+    // # newArray    
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