using Godot;
using System;

public partial class RPG : Node3D{

    public override void _Ready()
    {
        new TreeWalker("src/RPG.tw", new RPGModule(this)).Invoke("Main");
    }
}
