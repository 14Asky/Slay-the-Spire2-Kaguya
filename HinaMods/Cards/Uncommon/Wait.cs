using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 等待（Wait）
/// 本地化键：KAGUYA-SINGER_WAIT_BLOCK.title / .description
/// 2费 · 技能牌 · 罕见 · 目标：自身 · 关键词：歌声
/// 卡面：获得13点格挡。
///       下一张[purple]歌声[/purple]牌本回合内费用-1。
/// 代码实测：Block=13；CostReduce=1
/// 升级：CostReduce +1
/// </summary>
public sealed class Wait : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    public override List<CardKeyword> CanonicalKeywords => [KaguyaKeywords.Echo];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12m, ValueProp.Move),
        new DynamicVar("CostReduce", 1m)
    ];

    // 2费 技能 罕见
    public Wait()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 获得格挡
        await CommonActions.CardBlock(this, cardPlay);

        // 下一张回音牌本回合内费用减少
        int reduce = (int)DynamicVars["CostReduce"].BaseValue;
        await PowerCmd.Apply<SingerEchoCostDownPower>(
            choiceContext,
            player.Creature,
            reduce,
            player.Creature,
            this);
    }

    // 升级：格挡 12 → 15（费用减免恒为 1）
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
