using Godot;
using System;

public partial class RPG : Node3D{

    public override void _Ready(){
        var importTypes = new Type[]{typeof(Vector3), typeof(Color)};
        new TreeWalker("src/RPG.tw", new RPGModule(this), importTypes).Invoke("Main");
    }
}
