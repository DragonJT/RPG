using System.Collections.Generic;
using System;
using System.Linq;
using Godot;

enum ControlFlow{None, Break, Continue, Return}


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

    public VariableInstance Add(string name, dynamic value){
        var varInstance = new VariableInstance(value);
        variableStack[^1].Add(name, varInstance);
        return varInstance;
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
        else if(name == "Length"){
            return args[0].Length;
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
        if(node is Var var){
            funcStack.Peek().Add(var.name.value, Run(var.value));
            return null;
        }
        if(node is Assign assign){
            funcStack.Peek().TryGetValue(assign.name.value, out VariableInstance varInstance);
            varInstance.value = Run(assign.value);
            return null;
        }
        if(node is While @while){
            while(Run(@while.condition)){
                var returnValue = Run(@while.body);
                if(controlFlow == ControlFlow.Break){
                    controlFlow = ControlFlow.None;
                    return null;
                }
                else if(controlFlow == ControlFlow.Return){
                    return returnValue;
                }
            }
            return null;
        }
        if(node is For @for){
            var start = Run(@for.start);
            var end = Run(@for.end);
            funcStack.Peek().Push();
            var iterator = funcStack.Peek().Add(@for.varname.value, start);
            while(iterator.value<end){
                var returnValue = Run(@for.body);
                if(controlFlow == ControlFlow.Break){
                    controlFlow = ControlFlow.None;
                    funcStack.Peek().Pop();
                    return null;
                }
                else if(controlFlow == ControlFlow.Return){
                    funcStack.Peek().Pop();
                    return returnValue;
                }
                iterator.value += 1;
            }
            funcStack.Peek().Pop();
            return null;
        }
        if(node is If @if){
            if(Run(@if.condition)){
                var returnValue = Run(@if.body);
                if(controlFlow == ControlFlow.Return){
                    return returnValue;
                }
            }
            return null;
        }
        if(node is Break){
            controlFlow = ControlFlow.Break;
            return null;
        }
        if(node is Return @return){
            if(@return.expression == null){
                controlFlow = ControlFlow.Return;
                return null;
            }
            else{
                var returnValue = Run(@return.expression);
                controlFlow = ControlFlow.Return;
                return returnValue;
            }
        }
        if(node is Call call){
            var args = call.args.Select(a=>Run(a)).ToArray();
            return Invoke(call.name.value, args);
        }
        if(node is Indexor indexor){
            var index = Run(indexor.indexExpression);
            return funcStack.Peek().GetValue(indexor.varname.value)[index];
        }
        if(node is UnaryOp unaryOp){
            if(unaryOp.op.type == TokenType.Minus){
                return -Run(unaryOp.expression);
            }
            if(unaryOp.op.value == "!"){
                return !Run(unaryOp.expression);
            }
            else{
                throw new Exception("Unexpected unaryop: "+unaryOp.op.value);
            }
        }
        if(node is BinaryOp binaryOp){
            var left = binaryOp.left;
            var right = binaryOp.right;
            var returnValue = binaryOp.op.value switch
            {
                "+" => Run(left) + Run(right),
                "-" => Run(left) - Run(right),
                "*" => Run(left) * Run(right),
                "/" => Run(left) / Run(right),
                ">" => Run(left) > Run(right),
                "<" => Run(left) < Run(right),
                "<=" => Run(left) <= Run(right),
                ">=" => Run(left) >= Run(right),
                "!=" => Run(left) != Run(right),
                "==" => Run(left) == Run(right),
                "&&" => Run(left) && Run(right),
                "||" => Run(left) || Run(right),
                _ => throw new Exception("Unexpected binaryOp: " + binaryOp.op.value),
            };
            return returnValue;
        }
        if(node is Literal literal){
            return literal.type switch
            {
                LiteralType.Float => float.Parse(literal.value.value),
                LiteralType.Int => int.Parse(literal.value.value),
                LiteralType.String => literal.value.value,
                LiteralType.Char => literal.value.value[0],
                LiteralType.Variable => funcStack.Peek().GetValue(literal.value.value),
                LiteralType.True => true,
                LiteralType.False => false,
                _ => throw new Exception("Unexpected literaltype: " + literal.type.ToString()),
            };
        }
        throw new Exception("Unexpected tree type: "+node.GetType().Name);
    }
}