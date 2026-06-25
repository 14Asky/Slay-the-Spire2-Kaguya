using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 歌者防御卡牌：时光壁垒
/// 效果：获得抽牌堆、弃牌堆、消耗堆卡牌数量总和的格挡
/// 升级：费用减少1点（1费→0费）
/// </summary>
public sealed class MoonlightBarrier : HinaModsCard
{
    // 构造函数：2费、技能、普通稀有度、目标自身（完全保留）
    public MoonlightBarrier()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    // 保留你添加的【消耗】关键词（完全不动）
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 防御标签（完全保留）

    // 🔥 官方标准BlockVar，适配{Block:diff()}语法（完全保留）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(0m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获取玩家（完全保留）
        Player player = Owner;
        if (player == null) return;

        // 播放施法动画（完全保留）
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 核心：计算三堆卡牌总数（完全保留）
        int drawCount = PileType.Draw.GetPile(player).Cards.Count;
        int discardCount = PileType.Discard.GetPile(player).Cards.Count;
        int exhaustCount = PileType.Exhaust.GetPile(player).Cards.Count;
        int totalBlock = drawCount + discardCount + exhaustCount;

        // ======================
        // ✅ 修复报错：官方正确赋值方式
        // ======================
        // 修改BlockVar的基础值（基类BaseValue，反编译源码可访问）
        ((BlockVar)DynamicVars["Block"]).BaseValue = totalBlock;
        // 官方标准格挡调用，自动适配{Block:diff()}
        await CommonActions.CardBlock(this, cardPlay);
    }

    // 升级逻辑：费用-1（完全保留你写的代码）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
