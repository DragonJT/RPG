
using System.Collections.Generic;

class Variable{
    public Token name;

    public Variable(Token name){
        this.name = name;
    }
}

interface INode{}

interface IExpression:INode{}

enum LiteralType{Float, Int, String, Char, Variable, True, False}
class Literal:IExpression{
    public LiteralType type;
    public Token value;

    public Literal(LiteralType type, Token value){
        this.type = type;
        this.value = value;
    }

    public override string ToString(){
        return value.value;
    }
}

class BinaryOp:IExpression{
    public IExpression left;
    public IExpression right;
    public Token op;

    public BinaryOp(IExpression left, IExpression right, Token op){
        this.left = left;
        this.right = right;
        this.op = op;
    }

    public override string ToString() {
        return $"{left} {op.value} {right}";
    }
}

class UnaryOp:IExpression{
    public IExpression expression;
    public Token op;

    public UnaryOp(IExpression expression, Token op){
        this.expression = expression;
        this.op = op;
    }
}

class Call:IExpression{
    public Token name;
    public IExpression[] args;

    public Call(Token name, IExpression[] args){
        this.name = name;
        this.args = args;
    }
}

class New:IExpression{
    public Token name;
    public IExpression[] args;

    public New(Token name, IExpression[] args){
        this.name = name;
        this.args = args;
    }
}

interface IStatement:INode{}

class Expression:IStatement{
    public IExpression expression;

    public Expression(IExpression expression){
        this.expression = expression;
    }
}

class Indexor:IExpression{
    public Token varname;
    public IExpression indexExpression;

    public Indexor(Token varname, IExpression indexExpression){
        this.varname = varname;
        this.indexExpression = indexExpression;
    }
}

class Assign:IStatement{
    public Token name;
    public IExpression value;

    public Assign(Token name, IExpression value){
        this.name = name;
        this.value = value;
    }
}

class While:IStatement{
    public IExpression condition;
    public Body body;

    public While(IExpression condition, Body body){
        this.condition = condition;
        this.body = body;
    }
}

class For:IStatement{
    public Token varname;
    public IExpression start;
    public IExpression end;
    public Body body;

    public For(Token varname, IExpression start, IExpression end, Body body){
        this.varname = varname;
        this.start = start;
        this.end = end;
        this.body = body;
    }
}

class If:IStatement{
    public IExpression condition;
    public Body body;

    public If(IExpression condition, Body body){
        this.condition = condition;
        this.body = body;
    }
}

class Var:IStatement{
    public Token name;
    public IExpression value;

    public Var(Token name, IExpression value){
        this.name = name;
        this.value = value;
    }
}

class Global:IStatement{
    public Token name;
    public IExpression value;

    public Global(Token name, IExpression value){
        this.name = name;
        this.value = value;
    }
}

class Break:IStatement{
}

class Return:IStatement{
    public IExpression expression;

    public Return(IExpression expression){
        this.expression = expression;
    }
}

class Body:INode{
    public List<IStatement> statements;

    public Body(List<IStatement> statements){
        this.statements = statements;
    }
}

class Function{
    public Token name;
    public Variable[] parameters;
    public Body body;

    public Function(Token name, Variable[] parameters, Body body){
        this.name = name;
        this.parameters = parameters;
        this.body = body;
    }
}

class Tree{
    public List<Using> usings;
    public List<Function> functions;

    public Tree(List<Using> usings, List<Function> functions){
        this.usings = usings;
        this.functions = functions;
    }
}

class Using{
    public List<Token> tokens;

    public Using(List<Token> tokens){
        this.tokens = tokens;
    }
}