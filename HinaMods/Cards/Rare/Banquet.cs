using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Rare;

/// <summary>
/// 宴会（Banquet）
/// 本地化键：KAGUYA-SUPPORT_CHANNELING_CARD.title / .description
/// 1费 · 能力牌 · 罕见 · 目标：自身 · 标签：支援牌
/// 卡面：每回合开始时，随机将一张[blue]支援[/blue]牌加入[gold]手牌[/gold]。
/// 升级：该支援牌的费用额外 -1。
/// </summary>
public sealed class Banquet : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BanquetPower>(1m)
    ];

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    public Banquet() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
             HoverTipFactory.FromPower<BanquetPower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 官方施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await PowerCmd.Apply<BanquetPower>(
            choiceContext,
            player.Creature,
            DynamicVars[nameof(BanquetPower)].BaseValue,
            player.Creature,
            this);

        // 升级版宴会：每回合给的那张支援牌费用 -1
        BanquetPower power = player.Creature.GetPower<BanquetPower>();
        if (power != null && IsUpgraded)
        {
            power.ReduceCost = 1;
        }
    }

    // 升级效果体现在「给牌的减费」，无需改其它数值
    protected override void OnUpgrade()
    {
    }
}
