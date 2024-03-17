using Godot;

class RPGModule{
    Node3D node;

    public RPGModule(Node3D node){
        this.node = node;
    }

    public void AddBox(float x, float y, float z){
        var box = new CsgBox3D();
        node.AddChild(box);
        box.Position = new Vector3(x,y,z);
        box.Size = new Vector3(1,1,1);
    }

    public void AddCamera(float x, float y, float z, float lx, float ly, float lz){
        var camera = new Camera3D();
        node.AddChild(camera);
        camera.Position = new Vector3(x,y,z);
        camera.LookAt(new Vector3(lx,ly,lz));
    }
}