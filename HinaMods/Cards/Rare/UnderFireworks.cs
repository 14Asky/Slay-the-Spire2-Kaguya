using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 在烟花下（UnderFireworks）
/// 本地化键：KAGUYA-SUPPORT_FORGE_CARD.title / .description
/// 1费 · 能力牌 · 稀有 · 目标：自身
/// 卡面：每回合开始时，[gold]消耗[/gold]1张牌并获得1；
///       若选择的是[purple]歌声[/purple]牌，则打出该牌并再获得1。
/// 升级：无
/// </summary>
public sealed class UnderFireworks : HinaModsCard
{
    // 未升级带虚无；升级后回归基类关键词（无虚无）
    public override List<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? base.CanonicalKeywords.ToList() : [CardKeyword.Ethereal];

    // 1费 | 能力牌 | 稀有 | 指向自身
    public UnderFireworks()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<UnderFireworksPower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await PowerCmd.Apply<UnderFireworksPower>(
            choiceContext,
            player.Creature,
            1m,
            player.Creature,
            this);
    }

    // 升级：去除虚无（由 CanonicalKeywords 依据 IsUpgraded 判定，无需改数值）
    protected override void OnUpgrade()
    {
    }
}
