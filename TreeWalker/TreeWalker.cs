using System.Collections.Generic;
using System;
using System.Linq;
using Godot;

enum ControlFlow{None, Break, Continue, Return}


/*
class Literal:IExpression;
class BinaryOp:IExpression;
class UnaryOp:IExpression;
class Call:IExpression, IStatement;
class Assign:IStatement;
class Var:IStatement;
class Body;
class Function;
class Tree;*/

class VariableInstance{
    public dynamic value;

    public VariableInstance(dynamic value){
        this.value = value;
    }
}

class FunctionInstance{
    readonly List<Dictionary<string, VariableInstance>> variableStack = new();

    public FunctionInstance(){
        variableStack.Add(new());
    }

    public void Push(){
        variableStack.Add(new());
    }

    public void Pop(){
        variableStack.RemoveAt(variableStack.Count-1);
    }

    public void Add(string name, dynamic value){
        variableStack[^1].Add(name, new VariableInstance(value));
    }

    public bool TryGetValue(string name, out VariableInstance value){
        for(var i=variableStack.Count-1;i>=0;i--){
            if(variableStack[i].TryGetValue(name, out value)){
                return true;
            }
        }
        value = null;
        return false;
    }

    public dynamic GetValue(string name){
        if(TryGetValue(name, out VariableInstance value)){
            return value.value;
        }
        throw new Exception("Cant find variable with name: "+name);
    }
}

class TreeWalker{
    readonly Tree tree;
    ControlFlow controlFlow = ControlFlow.None;
    readonly Stack<FunctionInstance> funcStack = new();

    public TreeWalker(Tree tree){
        this.tree = tree;
    }

    public dynamic Invoke(string name, params dynamic[] args){
        if(name == "Print"){
            GD.Print(args[0].ToString());
            return null;
        }
        var function = tree.functions.First(t=>t.name.value == name);
        var funcInstance = new FunctionInstance();
        funcStack.Push(funcInstance);
        for(var i=0;i<args.Length;i++){
            funcInstance.Add(function.parameters[i].name.value, args[i]);
        }
        var returnValue = Run(function.body);
        funcStack.Pop();
        controlFlow = ControlFlow.None;
        return returnValue;
    }

    public dynamic Run(INode node){
        if(node is Body body){
            funcStack.Peek().Push();
            foreach(var s in body.statements){
                var returnValue = Run(s);
                if(controlFlow != ControlFlow.None){
                    funcStack.Peek().Pop();
                    return returnValue;
                }
            }
            funcStack.Peek().Pop();
            return null;
        }
        if(node is Var vr){
            funcStack.Peek().Add(vr.name.value, Run(vr.value));
            return null;
        }
        if(node is Assign assign){
            funcStack.Peek().TryGetValue(assign.name.value, out VariableInstance varInstance);
            varInstance.value = Run(assign.value);
            return null;
        }
        if(node is Call call){
            var args = call.args.Select(a=>Run(a)).ToArray();
            return Invoke(call.name.value, args);
        }
        if(node is UnaryOp unaryOp){
            if(unaryOp.op.value == "!"){
                return !Run(unaryOp.expression);
            }
            else{
                throw new Exception("Unexpected unaryop: "+unaryOp.op.value);
            }
        }
        if(node is BinaryOp binaryOp){
            var left = Run(binaryOp.left);
            var right = Run(binaryOp.right);
            return binaryOp.op.value switch
            {
                "+" => left + right,
                "-" => left - right,
                "*" => left * right,
                "/" => left / right,
                ">" => left > right,
                "<" => left < right,
                _ => throw new Exception("Unexpected binaryOp: " + binaryOp.op.value),
            };
        }
        if(node is Literal literal){
            return literal.type switch
            {
                LiteralType.Float => float.Parse(literal.value.value),
                LiteralType.Int => int.Parse(literal.value.value),
                LiteralType.String => literal.value.value,
                LiteralType.Char => literal.value.value[0],
                LiteralType.Variable => funcStack.Peek().GetValue(literal.value.value),
                _ => throw new Exception("Unexpected literaltype: " + literal.type.ToString()),
            };
        }
        throw new Exception("Unexpected tree type: "+node.GetType().Name);
    }
}