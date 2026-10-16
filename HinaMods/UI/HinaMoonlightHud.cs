#nullable enable
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Kaguya.HinaMods.UI;

/// <summary>
/// 月夜 HUD：一枚金色月亮，亮度随月夜层数递增，下方显示月夜实际层数。
/// 只由本地玩家自己的战斗界面创建一份（见 <see cref="HinaCombatHud"/>），
/// 多人模式下每个客户端只看到自己的那一份，互不冲突。
/// 基础设施类，无本地化键。
/// </summary>
public sealed partial class HinaMoonlightHud : Control
{
	private const string MoonIconPath = "res://images/hinamods/Powers/fortune_power.png";

	/// <summary>月夜层数达到该值时月亮达到最大亮度。</summary>
	private const float FullBrightStacks = 100f;

	private const float IconSize = 56f;
	private const float PanelWidth = 72f;
	private const float PanelHeight = 86f;

	/// <summary>金色基调；亮度靠 modulate 压暗/提亮。</summary>
	private static readonly Color MoonTint = new(1f, 0.93f, 0.56f);

	private Player? _player;
	private Texture2D? _moon;
	private Label? _value;
	private int _shownAmount = int.MinValue;
	private float _shownBrightness = -1f;

	public HinaMoonlightHud()
	{
	}

	public static HinaMoonlightHud Create(Player player)
	{
		// Size 必须在挂进场景树之前定好：BaseLib 的 Left 自动排版是按 Size 依次向上堆叠的
		return new HinaMoonlightHud
		{
			_player = player,
			Size = new Vector2(PanelWidth, PanelHeight),
		};
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Size = new Vector2(PanelWidth, PanelHeight);
		ClipContents = false;

		_moon = ResourceLoader.Load<Texture2D>(MoonIconPath);

		_value = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Position = new Vector2(0f, IconSize + 4f),
			Size = new Vector2(PanelWidth, PanelHeight - IconSize - 4f),
		};
		_value.AddThemeFontSizeOverride("font_size", 22);
		_value.AddThemeColorOverride("font_color", MoonTint);
		_value.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
		_value.AddThemeConstantOverride("outline_size", 6);
		_value.Text = "0";
		AddChild(_value);

		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		Creature? creature = ResolveCreature();
		if (creature == null)
		{
			// 只在真正需要切换时改 Visible，避免每帧触发 VisibilityChanged
			if (Visible)
			{
				Visible = false;
			}
			return;
		}

		if (!Visible)
		{
			Visible = true;
		}

		int amount = (int)(creature.GetPower<MoonlightPower>()?.Amount ?? 0m);
		float brightness = Mathf.Clamp(amount / FullBrightStacks, 0f, 1f);

		// 值没变就不动 Label、不重绘 —— 0 层时 amount 恒为 0，
		// 这里只会在「第一次进入 0 层」时写一次文本，之后完全静默。
		if (amount != _shownAmount && _value != null)
		{
			_shownAmount = amount;
			_value.Text = amount.ToString();
		}

		if (!Mathf.IsEqualApprox(brightness, _shownBrightness))
		{
			_shownBrightness = brightness;
			QueueRedraw();
		}
	}

	/// <summary>
	/// 显示条件：本地玩家 + 存活 + 战斗中。与"身上挂没挂某个 Power"完全解耦，
	/// 避免月夜被消耗到 0 层时 HUD 跟着消失。
	/// </summary>
	private Creature? ResolveCreature()
	{
		Player? player = _player;
		if (player == null)
		{
			return null;
		}

		// LocalContext.NetId 未建立时退化为放行，避免单机流程里误隐藏
		if (LocalContext.NetId.HasValue && !LocalContext.IsMe(player))
		{
			return null;
		}

		if (CombatManager.Instance is not { IsInProgress: true })
		{
			return null;
		}

		Creature? creature = player.Creature;
		if (creature == null || !creature.IsAlive)
		{
			return null;
		}

		return creature;
	}

	public override void _Draw()
	{
		if (_moon == null)
		{
			return;
		}

		float brightness = Mathf.Clamp(_shownBrightness < 0f ? 0f : _shownBrightness, 0f, 1f);

		// 无背景：只画图标本身。亮度靠 modulate —— 0 层压暗到 45%，满层全亮
		float level = Mathf.Lerp(0.45f, 1f, brightness);
		Vector2 center = new(PanelWidth * 0.5f, IconSize * 0.5f);
		var rect = new Rect2(center - Vector2.One * (IconSize * 0.5f), Vector2.One * IconSize);
		DrawTextureRect(_moon, rect, false, new Color(level * MoonTint.R, level * MoonTint.G, level * MoonTint.B, 1f));
	}
}
