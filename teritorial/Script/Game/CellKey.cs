namespace Teritorial.Game;

internal static class CellKey
{
	public static long Pack(int x, int y) => ((long)y << 32) | (uint)x;

	public static void Unpack(long key, out int x, out int y)
	{
		x = (int)(key & 0xFFFFFFFF);
		y = (int)(key >> 32);
	}
}
