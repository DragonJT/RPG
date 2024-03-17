using Godot;
using System;

public partial class WebBrowser : Control
{
    public override void _Ready()
	{
		var httpRequest = new HttpRequest();
		AddChild(httpRequest);
		httpRequest.RequestCompleted += HttpRequestCompleted;
		httpRequest.Request("https://godotengine.org/");
		var screenSize = DisplayServer.ScreenGetSize();
		GetWindow().Position = new Vector2I((int)(screenSize.X*0.1f),(int)(screenSize.X*0.05f));
        GetWindow().Size = new Vector2I(0,0);
		//GetWindow().Size = new Vector2I((int)(screenSize.X*0.8f), (int)(screenSize.Y*0.8f));
	}

	private void HttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
        var html = body.GetStringFromUtf8();
        var file = FileAccess.Open("test.tw", FileAccess.ModeFlags.Read);
	    var code = file.GetAsText();
        var tree = Parser.ParseTree(code);
        new TreeWalker(tree).Invoke("Main", html);

        //GD.Print(body.GetStringFromUtf8());
	}
}
