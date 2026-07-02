using System.Collections.Generic;
using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

// kdo ma jaky pixel, -1 = nikdo
public sealed class OwnershipGrid
{
	public const short Unowned = -1;

	public int Width { get; }
	public int Height { get; }

	private readonly short[] _owners;

	public OwnershipGrid(int width, int height)
	{
		Width = width;
		Height = height;
		_owners = new short[width * height];
		for (int i = 0; i < _owners.Length; i++)
			_owners[i] = Unowned;
	}

	public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

	public short GetOwner(int x, int y) =>
		InBounds(x, y) ? _owners[y * Width + x] : Unowned;

	public short GetOwnerFlat(int index) => _owners[index];

	public void SetOwner(int x, int y, short ownerId) => _owners[y * Width + x] = ownerId;

	// velikost startovniho fleku
	public static int TargetPixelsForRadius(int radius) =>
		Godot.Mathf.RoundToInt(Godot.Mathf.Pi * radius * radius * 0.85f);

	// bfs po pevnine kdy6 narazi na vodu hleda jinde
	public int FillLandBlob(int cx, int cy, int targetPixels, int maxRadius, short ownerId, TerrainGrid terrain)
	{
		if (!terrain.IsClaimable(cx, cy))
			return 0;

		int r2 = maxRadius * maxRadius;
		var visited = new bool[Width * Height];
		var queue = new Queue<Vector2I>();
		int startIdx = cy * Width + cx;
		visited[startIdx] = true;
		queue.Enqueue(new Vector2I(cx, cy));
		int count = 0;

		while (queue.Count > 0 && count < targetPixels)
		{
			var p = queue.Dequeue();
			if (!terrain.IsClaimable(p.X, p.Y))
				continue;

			int idx = p.Y * Width + p.X;
			if (_owners[idx] == Unowned)
			{
				_owners[idx] = ownerId;
				count++;
			}

			TryEnqueue(p.X + 1, p.Y);
			TryEnqueue(p.X - 1, p.Y);
			TryEnqueue(p.X, p.Y + 1);
			TryEnqueue(p.X, p.Y - 1);
		}

		return count;

		void TryEnqueue(int x, int y)
		{
			if (!InBounds(x, y) || !terrain.IsClaimable(x, y))
				return;

			int dx = x - cx;
			int dy = y - cy;
			if (dx * dx + dy * dy > r2)
				return;

			int i = y * Width + x;
			if (visited[i])
				return;

			visited[i] = true;
			queue.Enqueue(new Vector2I(x, y));
		}
	}

	// pro spawn kolik volne pevniny je pobliz
	public static int CountReachableLand(int cx, int cy, int maxRadius, TerrainGrid terrain)
	{
		if (!terrain.IsClaimable(cx, cy))
			return 0;

		int w = terrain.Width;
		int h = terrain.Height;
		int r2 = maxRadius * maxRadius;
		var visited = new bool[w * h];
		var queue = new Queue<Vector2I>();
		visited[cy * w + cx] = true;
		queue.Enqueue(new Vector2I(cx, cy));
		int count = 0;

		while (queue.Count > 0)
		{
			var p = queue.Dequeue();
			if (!terrain.IsClaimable(p.X, p.Y))
				continue;

			count++;
			TryEnqueue(p.X + 1, p.Y);
			TryEnqueue(p.X - 1, p.Y);
			TryEnqueue(p.X, p.Y + 1);
			TryEnqueue(p.X, p.Y - 1);
		}

		return count;

		void TryEnqueue(int x, int y)
		{
			if (!terrain.InBounds(x, y) || !terrain.IsClaimable(x, y))
				return;

			int dx = x - cx;
			int dy = y - cy;
			if (dx * dx + dy * dy > r2)
				return;

			int i = y * w + x;
			if (visited[i])
				return;

			visited[i] = true;
			queue.Enqueue(new Vector2I(x, y));
		}
	}

	public int CountOwnedBy(short ownerId)
	{
		int n = 0;
		for (int i = 0; i < _owners.Length; i++)
			if (_owners[i] == ownerId)
				n++;
		return n;
	}
}
