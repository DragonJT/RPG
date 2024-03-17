using Godot;
using System;

public partial class RPG : Node3D{
    TreeWalker treeWalker;

    public override void _Ready(){
        var importTypes = new Type[]{typeof(Vector3), typeof(Color)};
        treeWalker = new TreeWalker("src/RPG.tw", new RPGModule(this), importTypes);
        treeWalker.Invoke("Ready");
    }

    public override void _Input(InputEvent @event){
        treeWalker.Invoke("Input", @event);
    }
}
