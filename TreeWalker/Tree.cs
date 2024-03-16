
using System.Collections.Generic;

class Variable{
    public Token name;

    public Variable(Token name){
        this.name = name;
    }
}

interface INode{}

interface IExpression:INode{}

enum LiteralType{Float, Int, String, Char, Variable}
class Literal:IExpression{
    public LiteralType type;
    public Token value;

    public Literal(LiteralType type, Token value){
        this.type = type;
        this.value = value;
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
}

class UnaryOp:IExpression{
    public IExpression expression;
    public Token op;

    public UnaryOp(IExpression expression, Token op){
        this.expression = expression;
        this.op = op;
    }
}

interface IStatement:INode{}

class Call:IExpression, IStatement{
    public Token name;
    public IExpression[] args;

    public Call(Token name, IExpression[] args){
        this.name = name;
        this.args = args;
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

class Var:IStatement{
    public Token name;
    public IExpression value;

    public Var(Token name, IExpression value){
        this.name = name;
        this.value = value;
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
    public List<Function> functions;

    public Tree(List<Function> functions){
        this.functions = functions;
    }
}