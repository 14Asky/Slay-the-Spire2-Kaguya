using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 开幕（CurtainRaiser）
/// 本地化键：KAGUYA-SUPPORT_RHYTHM_CARD.title / .description
/// 1费 · 能力牌 · 罕见 · 目标：自身 · 标签：支援牌
/// 卡面：每打出{SupportThreshold:diff()}张[blue]支援[/blue]牌，抽1张牌。
/// 代码实测：SupportThreshold=4
/// 升级：SupportThreshold -1（4 → 3）
/// </summary>
public sealed class CurtainRaiser : HinaModsCard
{
    /// <summary>支援牌标签（本牌与支援牌体系联动）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    /// <summary>抽 1 张牌所需的支援牌张数（升级后 4 → 3）。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("SupportThreshold", 4m)
    ];

    public CurtainRaiser() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<CurtainRaiserPower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await PowerCmd.Apply<CurtainRaiserPower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            this);

        // 把「多少张支援牌抽 1 张」写进能力（基础 4 / 升级 3）
        CurtainRaiserPower power = player.Creature.GetPower<CurtainRaiserPower>();
        if (power != null)
        {
            power.Threshold = (int)DynamicVars["SupportThreshold"].BaseValue;
        }
    }

    // 升级：4 张 → 3 张
    protected override void OnUpgrade()
    {
        DynamicVars["SupportThreshold"].UpgradeValueBy(-1m);
    }
}
