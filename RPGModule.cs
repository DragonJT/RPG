using Godot;

class RPGModule{
    readonly Node3D node;

    public RPGModule(Node3D node){
        this.node = node;
    }

    public CsgBox3D AddBox(Vector3 position, Vector3 size, Color color){
        var box = new CsgBox3D();
        node.AddChild(box);
        box.Position = position;
        box.Size = size;
        var material = new StandardMaterial3D{
            AlbedoColor = color
        };
        box.Material = material;
        return box;
    }

    public DirectionalLight3D AddLight(Vector3 position, Vector3 lookAt){
        var light = new DirectionalLight3D();
        node.AddChild(light);
        light.LookAtFromPosition(position, lookAt);
        return light;
    }

    public Camera3D AddCamera(Vector3 position, Vector3 lookAt){
        var camera = new Camera3D();
        node.AddChild(camera);
        camera.LookAtFromPosition(position, lookAt);
        return camera;
    }

    public void Translate(CsgBox3D box, Vector3 delta){
        box.Position+=delta;
    }
}