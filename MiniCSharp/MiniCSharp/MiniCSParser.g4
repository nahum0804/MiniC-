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
                    
type
                    : simpleType                          # simpletype
                    | LIST LESS simpleType GREATER        # listOfSimple
                    | ident ( SBL SBR )?                  # userTypeOrArray
                    ;
                    
simpleType          : INT                                  # intType
                    | CHAR                                 # charType
                    | BOOL                                 # boolType
                    | STRING_TYPE                          # stringType
                    | FLOAT                                # floatType
                    ;
                    
statement           : designator ( ASSIGN expr | LEFTP ( actPars )? RIGHTP | ADD | SUB ) SEMICOLON          #assignStatement
            	    | IF LEFTP condition RIGHTP statement ( ELSE statement )?                               #ifStatement
            	    | FOR LEFTP forInit  SEMICOLON ( condition )? SEMICOLON ( forUpdate )? RIGHTP statement     #forStatement
            	    | WHILE LEFTP condition RIGHTP statement                                                #whileStatement
                    | BREAK SEMICOLON                                                                       #breakStatement
                    | RETURN ( expr )?  SEMICOLON                                                           #returnStatement
                    | READ LEFTP designator RIGHTP SEMICOLON                                                #readStatement
                    | WRITE LEFTP expr ( COMMA NUMLIT )? RIGHTP SEMICOLON                                   #writeStatement
                    | SWITCH LEFTP expr RIGHTP BL  caseBlock* SBL DEFAULT COLON statement* SBR BR SEMICOLON #switchStatement
                    | block                                                                                 #blackStatement
                    | SEMICOLON                                                                             #emptyStatement
                    ;
                    
forInit             :             #initEmpty
                    | designator ASSIGN expr    #initAssign
                    ;
                    
forUpdate           :             #updateEmpty
                    | designator ASSIGN expr    #updateAssign
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
                    
factor
                    : designator ( LEFTP ( actPars )? RIGHTP )?
                    | NUMLIT
                    | FLOATLIT                
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
                    
caseBlock           : CASE condition COLON statement*
                    ;

usingDecl           : USING ident ( DOT ident )* SEMICOLON
                    ;