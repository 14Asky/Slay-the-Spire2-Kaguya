using Kaguya.HinaMods.Powers;
using Kaguya.HinaMods.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 八千代的援助（YachiyosAid）
/// 本地化键：KAGUYA-MULTI_SUPPORT_GATHER.title / .description
/// 1费 · 技能牌 · 稀有 · 目标：AllAllies
/// 卡面：所有玩家获得1张[blue]招架[/blue]，1张[blue]疾驰[/blue]和1张[blue]支援打击[/blue]。
/// 升级：费用 -1
/// </summary>
public sealed class YachiyosAid : HinaModsCard
{

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    // 多人联机专属
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
        HoverTipFactory.FromCard<SupportParry>(),
        HoverTipFactory.FromCard<SupportDash>(),
        HoverTipFactory.FromCard<SupportStrike>()
        };
    }
    // 1费 技能牌 稀有 所有队友
    public YachiyosAid()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    { }

    // 官方原生遍历逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<Creature> teammates = from c in CombatState.GetTeammatesOf(Owner.Creature)
                                          where c != null && c.IsAlive && c.IsPlayer
                                          select c;

        foreach (Creature playerCreature in teammates)
        {
            // 1. 招架
            CardModel blockCard = CardScope.CreateCard<SupportParry>(playerCreature.Player);

            await CardPileCmd.Add(blockCard, PileType.Hand);

            // 2. 疾驰
            CardModel drawCard = CardScope.CreateCard<SupportDash>(playerCreature.Player);
            await CardPileCmd.Add(drawCard, PileType.Hand);

            // 🔥 新增：支援打击
            CardModel strikeCard = CardScope.CreateCard<SupportStrike>(playerCreature.Player);
            await CardPileCmd.Add(strikeCard, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}