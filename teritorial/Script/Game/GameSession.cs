namespace Teritorial.Game;

// volba mapy z menu, pak to nacte GameController
public static class GameSession
{
	public static string? SelectedMapPath { get; set; }

	public static void Clear() => SelectedMapPath = null;
}
