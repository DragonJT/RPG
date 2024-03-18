using Godot;
using System;

public partial class RPG : Node3D{
    TreeWalker treeWalker;

    public override void _Ready(){
        /*using var file = FileAccess.Open("src/RPG.tw", FileAccess.ModeFlags.Read);
        string code = file.GetAsText();
        var tree = Parser.ParseTree(code);
        treeWalker = new TreeWalker(tree, this);
        treeWalker.Invoke("Ready");*/

        using var file = FileAccess.Open("src/RPG.tw", FileAccess.ModeFlags.Read);
        string code = file.GetAsText();
        var tree = Parser.ParseTree(code);
        new CLREmitter(tree).Run();
    }

    public override void _Input(InputEvent @event){
        //treeWalker.Invoke("Input", @event);
    }
}