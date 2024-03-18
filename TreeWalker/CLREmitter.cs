using System.Reflection;
using System.Reflection.Emit;
using System;
using Godot;
using System.Linq;
using System.Collections.Generic;

class CLREmitter{
    Tree tree;

    public CLREmitter(Tree tree){
        this.tree = tree;
    }

    static bool CompareParameters(ParameterInfo[] parameters, Type[] args){
        bool hasParams = parameters[parameters.Length-1].GetCustomAttribute(typeof(ParamArrayAttribute), false)!=null;
        if(hasParams){
            for(var i=0;i<parameters.Length-1;i++){
                if(i>=args.Length){
                    return false;
                }
                if(!parameters[i].ParameterType.IsAssignableFrom(args[i])){
                    return false;
                }
            }
            var elementType = parameters[parameters.Length-1].ParameterType.GetElementType();
            for(var i=parameters.Length-1;i < args.Length; i++){
                if(!elementType.IsAssignableFrom(args[i])){
                    return false;
                }
            }
            return true;
        }
        else{
            for(var i=0;i<args.Length;i++){
                if(i>=parameters.Length){
                    return false;
                }
                if(!parameters[i].ParameterType.IsAssignableFrom(args[i])){
                    return false;
                }
            }
            for(var i=args.Length;i<parameters.Length;i++){
                if(!parameters[i].HasDefaultValue){
                    return false;
                }
            }
            return true;
        }
    }

    static bool FindMethod(Type type, string name, Type[] parameters, BindingFlags flags, out MethodInfo method){
        MethodInfo[] methods = type.GetMethods(flags)
            .Where(m=>m.Name == name && CompareParameters(m.GetParameters(), parameters))
            .ToArray();
        if(methods.Length > 0){
            method = methods[0];
            return true;
        }
        method = null;
        return false;
    }

    static MethodInfo FindMethod(Type type, string name, Type[] parameters, BindingFlags flags){
        if(FindMethod(type, name, parameters, flags, out MethodInfo method)){
            return method;
        }
        throw new Exception("Error cant find method: "+name);
    }

    Type EmitType(){
        var aName = new AssemblyName("DynamicAssemblyExample");
        AssemblyBuilder ab =
            AssemblyBuilder.DefineDynamicAssembly(
                aName,
                AssemblyBuilderAccess.Run);

        // The module name is usually the same as the assembly name.
        ModuleBuilder mb = ab.DefineDynamicModule(aName.Name ?? "DynamicAssemblyExample");

        TypeBuilder tb = mb.DefineType(
            "Global",
             TypeAttributes.Public);
        MethodBuilder func = tb.DefineMethod("Main", MethodAttributes.Public|MethodAttributes.Static, typeof(float), null);
        ILGenerator il = func.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Newarr, typeof(object));
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldc_R4, 5f);
        il.Emit(OpCodes.Box, typeof(float));
        il.Emit(OpCodes.Stelem_Ref);
        var staticFlags = BindingFlags.Public|BindingFlags.Static;
        il.EmitCall(OpCodes.Call, FindMethod(typeof(GD), "Print", new Type[]{typeof(object)}, staticFlags), null);
        il.Emit(OpCodes.Ldc_R4, 4f);
        il.Emit(OpCodes.Ret);
        return tb.CreateType();
    }

    public void Run(){
        var type = EmitType();
        var methodInfo = type.GetMethod("Main");
        GD.Print(methodInfo.Invoke(null, null));
    }
    
}