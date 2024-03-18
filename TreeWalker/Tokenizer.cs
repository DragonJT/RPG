using System.Collections.Generic;
using System;
using System.Linq;

enum TokenType{Varname, Int, Float, Curly, Square, Parens, Operator, DoubleQuote, Minus,
    SingleQuote, Equals, SemiColon, Comma, Var, Global, While, If, Break, True, False, Return, For, Using}

class Token{
    public string value;
    public int start;
    public int end;
    public TokenType type;

    public Token(string value, int start, int end, TokenType type){
        this.value = value;
        this.start = start;
        this.end = end;
        this.type = type;
    }
}

static class Tokenizer{
    static Token CreateVarname(string code, int start, int index, Dictionary<string, TokenType> varnameLiterals){
        var value = code[start..index];
        if(varnameLiterals.TryGetValue(value, out TokenType type)){
            return new Token(value, start, index, type);
        }
        return new Token(value, start, index, TokenType.Varname);
    }

    public static List<Token> Tokenize(string code){
        var index = 0;
        var tokens = new List<Token>();
        var open = "({[";
        var close = ")}]";
        var specialLiterals = new Dictionary<char, TokenType> {
             {'=', TokenType.Equals}, {';', TokenType.SemiColon}, {',', TokenType.Comma}, {'-', TokenType.Minus}
        };
        var varnameLiterals = new Dictionary<string, TokenType>{
            {"var", TokenType.Var}, {"global", TokenType.Global}, {"while", TokenType.While}, {"if", TokenType.If}, 
            {"break", TokenType.Break}, {"true", TokenType.True}, {"false", TokenType.False}, {"return", TokenType.Return},
            {"for", TokenType.For}, {"using", TokenType.Using }
        };
        var operators = "+*/<>!";
        var operators2 = new string[]{"==", ">=", "<=", "!=", "&&", "||"};

        loop:
        if(index>=code.Length){
            return tokens;
        }
        var c = code[index];
        if(char.IsWhiteSpace(c)){
            index++;
            goto loop;
        }
        if(char.IsLetter(c) || c=='_'){
            var start = index;
            index++;
            while(true){
                if(index>=code.Length){
                    tokens.Add(CreateVarname(code, start, index, varnameLiterals));
                    return tokens;
                }
                c = code[index];
                if(char.IsLetter(c) || c=='_' || char.IsDigit(c)){
                    index++;
                    continue;
                }
                tokens.Add(CreateVarname(code, start, index, varnameLiterals));
                goto loop;
            }
        }
        if(char.IsDigit(c)){
            var start = index;
            index++;
            var type = TokenType.Int;
            while(true){
                if(index>=code.Length){
                    tokens.Add(new Token(code[start..index], start, index, type));
                    return tokens;
                }
                c = code[index];
                if(c=='.'){
                    type = TokenType.Float;
                    index++;
                    continue;
                }
                else if(char.IsDigit(c)){
                    index++;
                    continue;
                }
                tokens.Add(new Token(code[start..index], start, index, type));
                goto loop;
            }
        }
        if(open.Contains(c)){
            var start = index;
            var depth = 1;
            index++;
            while(true){
                if(index>=code.Length){
                    throw new Exception("No close braces before end of file");
                }
                c = code[index];
                if(open.Contains(c)){
                    index++;
                    depth++;
                    continue;
                }
                else if(close.Contains(c)){
                    index++;
                    depth--;
                    if(depth<1){
                        if(code[start] == '{' && code[index-1]=='}'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Curly));
                            goto loop;
                        }
                        else if(code[start] == '[' && code[index-1]==']'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Square));
                            goto loop;
                        }
                        else if(code[start] == '(' && code[index-1]==')'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Parens));
                            goto loop;
                        }
                        else{
                            throw new Exception("Unexpected start and end of braces: "+code[start]+code[index-1]);
                        }
                    }
                    continue;
                }
                index++;
            }
        }
        if(c == '"'){
            var start = index;
            index++;
            while(true){
                if(index>=code.Length){
                    throw new Exception("Expecting end of doublequote");
                }
                c = code[index];
                if(c=='\\'){
                    index+=2;
                    continue;
                }
                else if(c=='"'){
                    index++;
                    tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.DoubleQuote));
                    goto loop;
                }
                index++;
            }
        }
        if(c == '\''){
            var start = index;
            index++;
            while(true){
                if(index>=code.Length){
                    throw new Exception("Expecting end of singlequote");
                }
                c = code[index];
                if(c=='\\'){
                    index+=2;
                    continue;
                }
                else if(c=='\''){
                    index++;
                    tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.SingleQuote));
                    goto loop;
                }
                index++;
            }
        }
        if(index+1<code.Length){
            var c2 = code.Substring(index, 2);
            if(operators2.Contains(c2)){
                tokens.Add(new Token(c2, index, index+2, TokenType.Operator));
                index+=2;
                goto loop;
            }
        }
        if(specialLiterals.TryGetValue(c, out TokenType value)){
            tokens.Add(new Token(c.ToString(), index, index+1, value));
            index++;
            goto loop;
        }
        if(operators.Contains(c)){
            tokens.Add(new Token(c.ToString(), index, index+1, TokenType.Operator));
            index++;
            goto loop;
        }
        throw new Exception("Unexpected character: "+code[index]);
    }
}