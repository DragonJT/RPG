using System.Collections.Generic;
using System.Linq;
using System;

static class Parser{
    static List<List<Token>> SplitByComma(List<Token> tokens){
        List<List<Token>> output = new();
        var start = 0;
        for(var i=0;i<tokens.Count;i++){
            if(tokens[i].type == TokenType.Comma){
                output.Add(tokens.GetRange(start, i-start));
                start = i+1;
            }
        }
        if(start<tokens.Count){
            output.Add(tokens.GetRange(start, tokens.Count-start));
        }
        return output;
    }

    static IExpression ParseExpressionInParens(Token token){
        return ParseExpression(Tokenizer.Tokenize(token.value));
    }

    static IExpression[] ParseArgs(Token token){
        return SplitByComma(Tokenizer.Tokenize(token.value)).Select(ParseExpression).ToArray();
    }

    static IExpression ParseExpression(List<Token> tokens){
        var operators = new string[][]{new string[]{"<", ">"}, new string[]{"+", "-"}, new string[]{"/", "*"}};
        if(tokens.Count == 0){
            throw new Exception("No tokens");
        }
        else if(tokens.Count == 1){
            if(tokens[0].type == TokenType.Parens){
                return ParseExpressionInParens(tokens[0]);
            }
            else if(tokens[0].type == TokenType.Float){
                return new Literal(LiteralType.Float, tokens[0]);
            }
            else if(tokens[0].type == TokenType.Int){
                return new Literal(LiteralType.Int, tokens[0]);
            }
            else if(tokens[0].type == TokenType.DoubleQuote){
                return new Literal(LiteralType.String, tokens[0]);
            }
            else if(tokens[0].type == TokenType.SingleQuote){
                return new Literal(LiteralType.Char, tokens[0]);
            }
            else if(tokens[0].type == TokenType.Varname){
                return new Literal(LiteralType.Variable, tokens[0]);
            }
            else if(tokens[0].type == TokenType.True){
                return new Literal(LiteralType.True, tokens[0]);
            }
            else if(tokens[0].type == TokenType.False){
                return new Literal(LiteralType.False, tokens[0]);
            }
            else{
                throw new Exception("Unexpected token: "+tokens[0]);
            }
        }
        else if(tokens.Count == 2){
            if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Parens){
                return new Call(tokens[0], ParseArgs(tokens[1]));
            }
        }
        foreach(var ops in operators){
            var index = tokens.FindLastIndex(t=>ops.Contains(t.value));
            if(index>=0){
                var left = ParseExpression(tokens.GetRange(0, index));
                var right = ParseExpression(tokens.GetRange(index+1, tokens.Count - (index+1)));
                return new BinaryOp(left, right, tokens[index]);
            }
        }
        if(tokens[0].type == TokenType.Operator && tokens[0].value == "!"){
            return new UnaryOp(ParseExpression(tokens.GetRange(1, tokens.Count-1)), tokens[0]);
        }
        throw new Exception("Unexpected tokens");
    }

    static List<List<Token>> SplitIntoStatements(List<Token> tokens){
        var output = new List<List<Token>>();
        var start = 0;
        for(var i=0;i<tokens.Count;i++){
            if(tokens[i].type == TokenType.SemiColon){
                output.Add(tokens.GetRange(start, i-start));
                start=i+1;
            }
            else if(tokens[i].type == TokenType.Curly){
                output.Add(tokens.GetRange(start, i+1-start));
                start=i+1;
            }
        }
        output.Add(tokens.GetRange(start, tokens.Count-start));
        return output;
    }

    static Body ParseBody(string code){
        var statementTokens = SplitIntoStatements(Tokenizer.Tokenize(code));
        List<IStatement> statements = new();
        for(var i=0;i<statementTokens.Count-1;i++){
            var s = statementTokens[i];
            if(s.Count>1 && s[1].type == TokenType.Equals){
                statements.Add(new Assign(s[0], ParseExpression(s.GetRange(2, s.Count-2))));
            }
            else if(s[0].type == TokenType.Var){
                statements.Add(new Var(s[1], ParseExpression(s.GetRange(3, s.Count-3))));
            }
            else if(s[0].type == TokenType.While){
                statements.Add(new While(ParseExpression(Tokenizer.Tokenize(s[1].value)), ParseBody(s[2].value)));
            }
            else if(s[0].type == TokenType.If){
                statements.Add(new If(ParseExpression(Tokenizer.Tokenize(s[1].value)), ParseBody(s[2].value)));
            }
            else if(s[0].type == TokenType.Break){
                statements.Add(new Break());
            }
            else if(s[0].type == TokenType.For){
                var args = SplitByComma(Tokenizer.Tokenize(s[1].value));
                statements.Add(new For(args[0][0], ParseExpression(args[1]), ParseExpression(args[2]), ParseBody(s[2].value)));
            }
            else if(s[0].type == TokenType.Return){
                if(s.Count == 1){
                    statements.Add(new Return(null));
                }
                else{
                    statements.Add(new Return(ParseExpression(s.GetRange(1, s.Count-1))));
                }
            }
            else if(s[0].type == TokenType.Varname && s[1].type == TokenType.Parens){
                statements.Add(new Call(s[0], ParseArgs(s[1])));
            }
            else{
                throw new Exception("SyntaxError: Unexpected statement");
            }
        }
        return new Body(statements);
    }

    static Variable[] ParseParameters(string code){
        var parameterTokens = SplitByComma(Tokenizer.Tokenize(code));
        return parameterTokens.Select(t=>new Variable(t[0])).ToArray();
    }

    public static Tree ParseTree(string code){
        var tokens = Tokenizer.Tokenize(code);
        var functions = new List<Function>();
        var i = 0;
        while(true){
            if(i>=tokens.Count){
                return new Tree(functions);
            }
            var name = tokens[i];
            var parameters = ParseParameters(tokens[i+1].value);
            functions.Add(new Function(name, parameters, ParseBody(tokens[i+2].value)));
            i+=4;
        }
    }
}
