using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using Kaguya.HinaMods.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using Kaguya.HinaMods.Cards;
using Kaguya.HinaMods.Extensions;
using Kaguya.HinaMods.Visuals;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System;
using System.Collections.Generic;
using Kaguya.HinaMods.Relics;

namespace Kaguya.HinaMods.Character;

// 自定义角色核心类，继承游戏官方占位符角色模型
/// <summary>
/// 辉夜（HinaCharacter）
/// 本地化键：KAGUYA-HINA_CHARACTER.title / .description
/// 卡面：她是好奇心旺盛的少女，从月亮而来，于七色游戏电线杆中现身。性格开朗爱撒娇，有时略显随心所欲。遇彩叶后，她理解了人类情感、懂得爱与抗争，曾回月亮，却因彩叶的歌声决心重返地球。
/// </summary>
public class HinaCharacter: PlaceholderCharacterModel
{
	// 角色唯一ID
	public const string CharacterId = "HinaMods";

	// 自定义角色选择界面背景（已注释禁用）
	public override string CustomCharacterSelectBg => "res://scenes/hinamods/Screens/char_select/char_select_bg_hina_mods.tscn";

	// 占位符资源ID：使用游戏内死灵缚者的基础资源作为占位
	public override string PlaceholderID => "regent";

	// 角色主题色
	public static readonly Color Color = new Color("#f3d141");
	// 角色名称颜色
	public override Color NameColor => Color;
	// 角色性别：女性
	public override CharacterGender Gender => CharacterGender.Feminine;
	//能量
	//public override Color EnergyLabelOutlineColor => new("##fbeed3");

	// 能量表盘tscn路径。要自定义见下。
	public override string CustomEnergyCounterPath => "res://scenes/hinamods/hinamods_energy_counter.tscn";
	// 初始生命值
	public override int StartingHp => 75;

	// 初始卡组：2张攻击牌 + 1张防御牌
	public override IEnumerable<CardModel> StartingDeck => [
		ModelDb.Card<Strike>(),
		ModelDb.Card<Strike>(),
		ModelDb.Card<Strike>(),
		ModelDb.Card<Strike>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<ActCute>(),
		ModelDb.Card<Playful>(),
		
	];

	// 初始遗物：燃烧之血
	// 原代码：燃烧之血
	// public override IReadOnlyList<RelicModel> StartingRelics => new List<RelicModel> { ModelDb.GetById<RelicModel>(new ModelId("RELIC", "BURNING_BLOOD")) }.AsReadOnly();

	public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<HinaModsFortuneRelic>()];


	// 绑定自定义卡牌池
	public override CardPoolModel CardPool => ModelDb.CardPool<HinaModsCardPool>();

	// 绑定通用遗物池
	public override RelicPoolModel RelicPool => ModelDb.RelicPool<HinaModsRelicPool>();
	// 绑定通用药水池
	public override PotionPoolModel PotionPool => ModelDb.PotionPool<HinaModsPotionPool>();

	/*  占位符角色模型会使用游戏本体的基础资源，直到你重写所有资源定义方法
		这里仅配置了最基础的资源作为占位，用来区分自定义角色
		非必须，但建议重命名这些图片资源 */
	// 自定义角色视觉模型路径
	// Spine 版（骨架数据 scenes/hinamods/spine/kaguya_spine_data.tres，装配逻辑见 HinaMods/Visuals/KaguyaSpineVisual.cs）
	public override string CustomVisualPath => KaguyaSpineVisual.ScenePath;
	// 旧的序列帧版立绘：Spine 装配失败时自动回退到这里
	public const string LegacyVisualPath = KaguyaSpineVisual.LegacyScenePath;

	/// <summary>
	/// 手动创建立绘节点（Spine 版，失败自动回退旧序列帧版）。
	/// 场景里的 Visuals 只是普通 Node2D 占位，真正的 SpineSprite 在节点入树前创建并替换——
	/// 这样 KaguyaSpine.tscn 里不会出现 SpineSprite 这个扩展类型，未装编辑器扩展的人
	/// 打开场景也不会看到「节点类型不可用」的占位提示。
	/// 替换必须早于入树：引擎 NCreatureVisuals._Ready 会立刻校验 %Visuals 是 SpineSprite
	/// 且骨架已加载，否则永久关闭 spine 动画；而入树过程中父节点 busy，add_child 会失败。
	/// </summary>
	public override NCreatureVisuals? CreateCustomVisuals()
	{
		return KaguyaSpineVisual.CreateVisuals();
	}

	/// <summary>
	/// Spine 立绘的动画名映射。
	/// kaguya.skel 里只有 idle_loop / idle_loop_backup / attack / cast / hurt / die，
	/// 没有引擎默认要的 low_health_loop / relaxed_loop，
	/// 所以低血量与放松状态统一回落到 idle_loop，其余按原名对应。
	/// </summary>
	public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
	{
		return SetupAnimationState(
			controller,
			idleName: AnimState.idleAnim,
			deadName: AnimState.dieAnim,
			hitName: AnimState.hurtAnim,
			attackName: AnimState.attackAnim,
			castName: AnimState.castAnim);
	}
	// 自定义角色图标路径
	public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();

	public override string CustomIconPath => "res://scenes/hinamods/ui/character_icons/character_icon_char_name.tscn";
	// 角色选择界面图标路径
	public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
	// 角色选择界面锁定图标路径
	public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
	// 大地图标记图标路径
	public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();

	// 卡牌左上角费用数字的描边颜色
	public override Color EnergyLabelOutlineColor => Colors.Black;
	// 地图上该角色绘制连线时使用的颜色
	public override Color MapDrawingColor => Colors.Gold;

	// 联机状态下，这个角色的指向线主体颜色
	public override Color RemoteTargetingLineColor => Colors.Gold;

	// 联机指向线的外描边颜色
	public override Color RemoteTargetingLineOutline => Colors.Gold;
	// 卡牌拖尾场景。
	// public override string CustomTrailPath => "res://scenes/vfx/card_trail_ironclad.tscn";
	// 能量表盘tscn路径。要自定义见下。
	//public override string CustomEnergyCounterPath => "res://test/scenes/test_energy_counter.tscn";
	// 篝火休息场景。
	public override string CustomRestSiteAnimPath => "res://scenes/hinamods/rest_site/characters/hinamods_kaguya_rest_site.tscn";
	// 商店人物场景。
	public override string CustomMerchantAnimPath => "res://scenes/hinamods/ui/hina_mods_mechant.tscn";
	// 多人模式-手指。
	public override string CustomArmPointingTexturePath => "res://scenes/hinamods/ui/hands/multiplayer_hand_kaguya_point.png";
	//多人模式剪刀石头布-石头。
	public override string CustomArmRockTexturePath => "res://scenes/hinamods/ui/hands/multiplayer_hand_kaguya_rock.png";
	//多人模式剪刀石头布-布。
	public override string CustomArmPaperTexturePath => "res://scenes/hinamods/ui/hands/multiplayer_hand_kaguya_paper.png";
	//多人模式剪刀石头布-剪刀。
	 public override string CustomArmScissorsTexturePath => "res://scenes/hinamods/ui/hands/multiplayer_hand_kaguya_scissors.png";
	// 人物选择过渡动画。
	// public override string CustomCharacterSelectTransitionPath => "res://materials/transitions/ironclad_transition_mat.tres";
	// 地图上的角色标记图标、表情轮盘上的角色头像
	// public override string CustomMapMarkerPath => null;
	// 攻击音效
	// public override string CustomAttackSfx => null;
	// 施法音效
	// public override string CustomCastSfx => null;
	// 死亡音效
	// public override string CustomDeathSfx => null;
	// 角色选择音效
	// public override string CharacterSelectSfx => null;
	// 过渡音效。这个不能删。
	//public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";
	// 攻击建筑师的攻击特效列表
	public override List<string> GetArchitectAttackVfx() => [
		"vfx/vfx_attack_blunt",
		"vfx/vfx_heavy_blunt",
		"vfx/vfx_attack_slash",
		"vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter"
	];

}
