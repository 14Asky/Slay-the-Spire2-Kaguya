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
/// 隔世（AnotherWorld）
/// 本地化键：KAGUYA-MOONLIGHT_BARRIER.title / .description
/// 1费 · 技能牌 · 稀有 · 目标：自身 · 关键词：Exhaust
/// 卡面：获得[gold]抽牌堆[/gold]，[gold]弃牌堆[/gold]，[gold]消耗堆[/gold]中卡牌总数点格挡。
/// 代码实测：Block=0
/// 升级：费用 -1
/// </summary>
public sealed class AnotherWorld : HinaModsCard
{
    // 1费 | 技能 | 稀有 | 指向自身
    public AnotherWorld()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    // 【消耗】
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 官方标准 BlockVar，适配 {Block:diff()} 语法
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(0m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 三堆卡牌数量总和
        int drawCount = PileType.Draw.GetPile(player).Cards.Count;
        int discardCount = PileType.Discard.GetPile(player).Cards.Count;
        int exhaustCount = PileType.Exhaust.GetPile(player).Cards.Count;
        int totalBlock = drawCount + discardCount + exhaustCount;

        // 格挡值只有打出时才算得出来：先写入 BlockVar，再走官方格挡结算
        ((BlockVar)DynamicVars["Block"]).BaseValue = totalBlock;
        await CommonActions.CardBlock(this, cardPlay);
    }

    // 升级：费用 -1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
