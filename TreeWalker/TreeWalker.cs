using System.Collections.Generic;
using System;
using System.Linq;
using Godot;
using System.Reflection;
using System.Threading;

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

    public TreeWalker(Tree tree, object node){
        this.tree = tree;
        globals.Add("node", new VariableInstance(node));
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

    bool FindVariable(string name, out VariableInstance varInstance){
        if(funcStack.Peek().TryGetValue(name, out varInstance)){
            return true;
        }
        if(globals.TryGetValue(name, out varInstance)){
            return true;
        }
        return false;
    }

    dynamic FindVariable(string name){
        if(FindVariable(name, out VariableInstance varInstance)){
            return varInstance.value;
        }
        throw new Exception("Error cant find variable with name: "+name);
    }

    dynamic Assign(IExpression left, IExpression right){
        if(left is Literal literal && literal.type == LiteralType.Varname){
            if(funcStack.Peek().TryGetValue(literal.value.value, out VariableInstance varInstance)){
                varInstance.value = Run(right);
            }
            else if(globals.TryGetValue(literal.value.value, out VariableInstance globalInstance)){
                globalInstance.value = Run(right);
            }
            return null;
        }
        if(left is BinaryOp binaryOp && binaryOp.op.value == "."){
            if(binaryOp.left is Literal literalLeft){
                if(FindVariable(literalLeft.value.value, out VariableInstance varInstance)){
                    var type = (Type)varInstance.value.GetType();
                    if(binaryOp.right is Literal literalRight){
                        var property = type.GetProperty(literalRight.value.value);
                        property.SetValue(varInstance.value, Run(right));
                        return null;
                    }
                    else{
                        throw new NotImplementedException();
                    }
                }
                else{
                    throw new NotImplementedException();
                }
            }
            
        }
        throw new Exception("cant assign to: "+left.ToString());        
    }

    Type FindType(string name){
        if(allTypes.TryGetValue(name, out List<Type> types)){
            var finalTypes = types.Where(t=>t.DeclaringType == null && (t.Namespace == null || UsingsContains(t.Namespace))).ToArray();
            if(finalTypes.Length == 1){
                return finalTypes[0];
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

    dynamic[] RunArgs(Call call){
        return call.args.Select(a=>Run(a)).ToArray();
    }

    static bool MatchingArgsAndParameters(ParameterInfo[] parameters, dynamic[] args){
        if(parameters.Length < args.Length){
            return false;
        }
        for(var i=0;i<parameters.Length;i++){
            if(parameters[i].DefaultValue == DBNull.Value){
                if(i>=args.Length){
                    return false;
                }
                if(!parameters[i].ParameterType.IsAssignableFrom((Type)args[i].GetType())){
                    return false;
                }
            }
            else{
                if(i<args.Length && !parameters[i].ParameterType.IsAssignableFrom((Type)args[i].GetType())){
                    return false;
                }
            }
        }
        return true;
    }

    MethodInfo FindMethodInHierarchy(Type type, dynamic instance, string name, dynamic[] args){
        var flags = instance==null?
            BindingFlags.Static|BindingFlags.Public:
            BindingFlags.Instance|BindingFlags.Public;
        var methods = type.GetMethods(flags)
                .Where(m=>m.Name == name)
                .Where(m=>MatchingArgsAndParameters(m.GetParameters(), args))
                .ToArray();
        if(methods.Length > 0){
            return methods[0];
        }
        if(type.BaseType!=null){
            return FindMethodInHierarchy(type.BaseType, instance, name, args);
        }
        throw new Exception("Cant find method with name in type or basetype: ");
    }

    dynamic TypeDot(IExpression right, Type type, dynamic instance){
        if(right is Call call){
            var args = RunArgs(call);
            var method = FindMethodInHierarchy(type, instance, call.name.value, args);
            List<dynamic> finalArgs = new();
            var parameters = method.GetParameters();
            for(var i=0;i<parameters.Length;i++){
                if(i<args.Length){
                    finalArgs.Add(args[i]);
                }
                else{
                    finalArgs.Add(parameters[i].DefaultValue);
                }
            }
            return method.Invoke(instance, finalArgs.ToArray());
        }
        else if(right is Literal literal){
            var property = type.GetProperty(literal.value.value);
            return property.GetValue(instance);
        }
        else{
            throw new NotImplementedException();
        }
    }

    dynamic Dot(IExpression left, IExpression right){
        if(left is Literal literal && literal.type == LiteralType.Varname){
            var name = literal.value.value;
            if(FindVariable(name, out VariableInstance varInstance)){
                return TypeDot(right, (Type)varInstance.value.GetType(), varInstance.value);
            }
            else{
                return TypeDot(right, FindType(name), null);
            }
        }
        else{
            throw new NotImplementedException();
        }
    }

    dynamic InvokeNew(string name, params dynamic[] args){
        var type = FindType(name);
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var constructor = constructors.FirstOrDefault(c=>c.GetParameters().Length == args.Length);
        return constructor.Invoke(args);
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
            return Invoke(call.name.value, RunArgs(call));
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
            return FindVariable(indexor.varname.value)[index];
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
                "." => Dot(left, right),
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
                LiteralType.Varname => FindVariable(literal.value.value),
                LiteralType.True => true,
                LiteralType.False => false,
                _ => throw new Exception("Unexpected literaltype: " + literal.type.ToString()),
            };
        }
        throw new Exception("Unexpected tree type: "+node.GetType().Name);
    }
}