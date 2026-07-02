using Godot;

namespace Teritorial.Game;

public partial class GameHud : CanvasLayer
{
	private PanelContainer _panel = null!;
	private Label _popLabel = null!;
	private Label _landLabel = null!;
	private Label _attackLabel = null!;
	private Label _mapLabel = null!;
	private Label _statusLabel = null!;
	private Label _sendLabel = null!;
	private HSlider _sendSlider = null!;
	private HBoxContainer _confirmRow = null!;
	private Button _confirmBtn = null!;
	private Button _cancelBtn = null!;

	private PanelContainer _endPanel = null!;
	private Label _endTitle = null!;
	private Label _endMessage = null!;
	private Button _restartBtn = null!;
	private Button _menuBtn = null!;

	public float SendPercent => (float)_sendSlider.Value;

	public override void _Ready()
	{
		_panel = new PanelContainer { Position = new Vector2(12, 12) };
		AddChild(_panel);

		var box = new VBoxContainer();
		_panel.AddChild(box);

		_popLabel = new Label();
		_landLabel = new Label();
		_attackLabel = new Label();
		_mapLabel = new Label();
		_statusLabel = new Label { Text = "LMB = expanze/útok  |  RMB = námořní útok" };
		_sendLabel = new Label { Text = "Expedice (% populace):" };
		_sendSlider = new HSlider
		{
			MinValue = 1,
			MaxValue = 100,
			Value = 25,
			CustomMinimumSize = new Vector2(220, 0),
		};

		_confirmRow = new HBoxContainer { Visible = false };
		_confirmBtn = new Button { Text = "Potvrdit námořní útok" };
		_cancelBtn = new Button { Text = "Zrušit" };
		_confirmRow.AddChild(_confirmBtn);
		_confirmRow.AddChild(_cancelBtn);

		box.AddChild(_popLabel);
		box.AddChild(_landLabel);
		box.AddChild(_attackLabel);
		box.AddChild(_mapLabel);
		box.AddChild(_statusLabel);
		box.AddChild(_sendLabel);
		box.AddChild(_sendSlider);
		box.AddChild(_confirmRow);

		_sendSlider.ValueChanged += _ => UpdateSendText();
		UpdateSendText();

		_endPanel = new PanelContainer
		{
			Visible = false,
			AnchorsPreset = (int)Control.LayoutPreset.Center,
			OffsetLeft = -200,
			OffsetTop = -80,
			OffsetRight = 200,
			OffsetBottom = 80,
		};
		AddChild(_endPanel);

		var endBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_endPanel.AddChild(endBox);
		_endTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_endMessage = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(360, 0),
		};
		_restartBtn = new Button { Text = "Hrát znovu" };
		_menuBtn = new Button { Text = "Hlavní menu" };
		var endButtons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		endButtons.AddChild(_restartBtn);
		endButtons.AddChild(_menuBtn);
		endBox.AddChild(_endTitle);
		endBox.AddChild(_endMessage);
		endBox.AddChild(endButtons);
	}

	public void BindConfirm(System.Action onConfirm, System.Action onCancel)
	{
		_confirmBtn.Pressed += onConfirm;
		_cancelBtn.Pressed += onCancel;
	}

	public void BindRestart(System.Action onRestart) => _restartBtn.Pressed += onRestart;

	public void BindMainMenu(System.Action onMainMenu) => _menuBtn.Pressed += onMainMenu;

	public bool IsMouseOverUi(Vector2 screenPos)
	{
		if (_endPanel.Visible && _endPanel.GetGlobalRect().HasPoint(screenPos))
			return true;
		return _panel.GetGlobalRect().HasPoint(screenPos);
	}

	public void ShowNavalConfirm(bool show) => _confirmRow.Visible = show;

	public void UpdatePlayerStats(
		float population,
		int landPixels,
		float growthPerSec,
		float maxPop,
		int activeAttacks,
		int maxAttacks,
		float mapControlPercent,
		float winTargetPercent)
	{
		int pct = maxPop > 0 ? (int)(population / maxPop * 100f) : 0;
		_popLabel.Text = $"Populace: {population:0} / {maxPop:0} ({pct}%)";
		_landLabel.Text = $"Území: {landPixels} px  |  růst: {growthPerSec:0.1}/s";
		_attackLabel.Text = $"Aktivní útoky: {activeAttacks} / {maxAttacks}";
		_mapLabel.Text = $"Mapa: {mapControlPercent * 100f:0.0}%  (cíl {winTargetPercent * 100f:0}%)";
	}

	public void SetStatus(string text) => _statusLabel.Text = text;

	public void ShowEndScreen(GameOutcome outcome)
	{
		_endTitle.Text = outcome.IsWin ? "Vítězství!" : "Prohra";
		_endMessage.Text = outcome.Message;
		_endPanel.Visible = true;
		_panel.Visible = false;
	}

	private void UpdateSendText()
	{
		_sendLabel.Text = $"Expedice (% populace): {(int)_sendSlider.Value}";
	}
}
