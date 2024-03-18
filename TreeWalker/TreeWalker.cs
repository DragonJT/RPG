using System.Collections.Generic;
using System;
using System.Linq;
using Godot;
using System.Reflection;

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
}

class TreeWalker{
    readonly Tree tree;
    ControlFlow controlFlow = ControlFlow.None;
    readonly Stack<FunctionInstance> funcStack = new();
    readonly Dictionary<string, VariableInstance> globals = new();
    readonly Dictionary<string, List<Type>> allTypes = new(); 
    readonly object module;

    public TreeWalker(Tree tree, object module){
        this.tree = tree;
        this.module = module;
        foreach(var a in AppDomain.CurrentDomain.GetAssemblies()){
            foreach(var t in a.GetTypes()){
                if(allTypes.TryGetValue(t.Name, out List<Type> types)){
                    types.Add(t);
                }
                else{
                    allTypes.Add(t.Name, new List<Type>{t});
                }
            }
        }
    }

    bool UsingsContains(string @namespace){
        foreach(var u in tree.usings){
            var name = "";
            foreach(var token in u.tokens){
                name+=token.value;
            }
            if(name == @namespace){
                return true;
            }
        }
        return false;
    }

    dynamic GetVariable(string name){
        if(funcStack.Peek().TryGetValue(name, out VariableInstance varInstance)){
            return varInstance.value;
        }
        if(globals.TryGetValue(name, out VariableInstance globalInstance)){
            return globalInstance.value;
        }
        throw new Exception("Expecting variable with name: "+name);
    }

    dynamic Assign(IExpression left, IExpression right){
        if(left is Literal literal && literal.type == LiteralType.Variable){
            if(funcStack.Peek().TryGetValue(literal.value.value, out VariableInstance varInstance)){
                varInstance.value = Run(right);
            }
            else if(globals.TryGetValue(literal.value.value, out VariableInstance globalInstance)){
                globalInstance.value = Run(right);
            }
            return null;
        }
        throw new Exception("cant assign to: "+left.ToString());        
    }

    dynamic InvokeNew(string name, params dynamic[] args){
        if(allTypes.TryGetValue(name, out List<Type> types)){
            var finalTypes = types.Where(t=>t.DeclaringType == null && (t.Namespace == null || UsingsContains(t.Namespace))).ToArray();
            if(finalTypes.Length == 1){
                var constructors = finalTypes[0].GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                var constructor = constructors.FirstOrDefault(c=>c.GetParameters().Length == args.Length);
                return constructor.Invoke(args);
            }
            else{
                if(finalTypes.Length == 0){
                    throw new Exception("Cnat find type with name: "+name+". Are you missing a using");
                }
                else{
                    throw new Exception("More than 1 available type: "+new string(finalTypes.SelectMany(t=>t.FullName+", ").ToArray()));
                }
            }
        }
        throw new Exception("Cant find type with name: "+name);
    }

    public dynamic Invoke(string name, params dynamic[] args){
        var function = tree.functions.FirstOrDefault(t=>t.name.value == name);
        if(function!=null){
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

        if(name == "Print"){
            GD.Print(args[0].ToString());
            return null;
        }
        else if(name == "Length"){
            return args[0].Length;
        }
        var methods = module.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var method = methods.FirstOrDefault(m=>m.Name == name);
        if(method!=null){
            return method.Invoke(module, args);
        }
        throw new Exception("Cant find function with name: "+name);
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
        if(node is Global global){
            globals.Add(global.name.value, new VariableInstance(Run(global.value)));
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
        if(node is New @new){
            var args = @new.args.Select(a=>Run(a)).ToArray();
            return InvokeNew(@new.name.value, args);
        }
        if(node is Expression expression){
            Run(expression.expression);
            return null;
        }
        if(node is Indexor indexor){
            var index = Run(indexor.indexExpression);
            return GetVariable(indexor.varname.value)[index];
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
                "=" => Assign(left, right),
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
                LiteralType.Variable => GetVariable(literal.value.value),
                LiteralType.True => true,
                LiteralType.False => false,
                _ => throw new Exception("Unexpected literaltype: " + literal.type.ToString()),
            };
        }
        throw new Exception("Unexpected tree type: "+node.GetType().Name);
    }
}