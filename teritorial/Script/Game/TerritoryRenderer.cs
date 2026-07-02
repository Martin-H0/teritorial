using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

// barevna vrstva uzemi pres mapu
public partial class TerritoryRenderer : Sprite2D
{
	private Image _image = null!;
	private ImageTexture _texture = null!;

	public void Setup(int width, int height)
	{
		_image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
		_texture = ImageTexture.CreateFromImage(_image);
		Texture = _texture;
		Centered = false;
		Position = Vector2.Zero;
	}

	public void Rebuild(OwnershipGrid ownership, TerrainGrid terrain)
	{
		for (int y = 0; y < ownership.Height; y++)
		for (int x = 0; x < ownership.Width; x++)
		{
			if (!terrain.IsClaimable(x, y))
			{
				_image.SetPixel(x, y, Colors.Transparent);
				continue;
			}

			short owner = ownership.GetOwner(x, y);
			if (owner == OwnershipGrid.Unowned)
			{
				_image.SetPixel(x, y, Colors.Transparent);
				continue;
			}

			var c = PlayerColors.Get(owner);
			c.A = 0.72f;
			_image.SetPixel(x, y, c);
		}

		_texture.Update(_image);
	}
}
