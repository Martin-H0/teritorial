using Godot;

namespace Teritorial.Game;

// WASD sipky zoom kolecko
public partial class CameraController : Camera2D
{
	[Export] public float PanSpeed = 480f;
	[Export] public float ZoomStep = 0.12f;
	[Export] public float ZoomMin = 0.4f;
	[Export] public float ZoomMax = 3.5f;
	[Export] public Vector2 MapSize = new(512, 512);

	public override void _Process(double delta)
	{
		var dir = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
			dir.Y -= 1;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
			dir.Y += 1;
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
			dir.X -= 1;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
			dir.X += 1;

		if (dir != Vector2.Zero)
		{
			Position += dir.Normalized() * PanSpeed * (float)delta / Zoom.X;
			ClampToMap();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton wheel || !wheel.Pressed)
			return;

		if (wheel.ButtonIndex == MouseButton.WheelUp)
			SetZoomLevel(Zoom.X * (1f + ZoomStep));
		else if (wheel.ButtonIndex == MouseButton.WheelDown)
			SetZoomLevel(Zoom.X * (1f - ZoomStep));
		else
			return;

		GetViewport().SetInputAsHandled();
	}

	private void SetZoomLevel(float z)
	{
		z = Mathf.Clamp(z, ZoomMin, ZoomMax);
		Zoom = new Vector2(z, z);
		ClampToMap();
	}

	private void ClampToMap()
	{
		var viewport = GetViewportRect().Size / Zoom.X;
		float minX = viewport.X * 0.5f;
		float minY = viewport.Y * 0.5f;
		float maxX = MapSize.X - viewport.X * 0.5f;
		float maxY = MapSize.Y - viewport.Y * 0.5f;

		if (maxX < minX)
			Position = new Vector2(MapSize.X * 0.5f, Position.Y);
		else
			Position = new Vector2(Mathf.Clamp(Position.X, minX, maxX), Position.Y);

		if (maxY < minY)
			Position = new Vector2(Position.X, MapSize.Y * 0.5f);
		else
			Position = new Vector2(Position.X, Mathf.Clamp(Position.Y, minY, maxY));
	}
}
