using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 电子幽灵（DigitalGhost）
/// 本地化键：KAGUYA-SUPPORT_INTANGIBLE.title / .description
/// 3费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：[blue]支援[/blue]：获得1层[gold]无实体[/gold]。
/// 代码实测：IntangiblePower=1
/// 升级：IntangiblePower +1
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class DigitalGhost : HinaModsCard
{
    // 支援 + 支援牌标签（支援标签使基类自动附加虚无/消耗关键词）
    public override HashSet<string> CustomTags =>
        new HashSet<string>() { CustomCardTags.SUPPORT, CustomCardTags.SUPPORTCARD };

    // 动态变量：无实体层数（基础 1，升级 2）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<IntangiblePower>(1m)
    ];

    // 标准支援牌构造：3费 技能 代币卡 自身目标
    public DigitalGhost() : base(3, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 核心效果：获得无实体 + 施加支援之力BUFF
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IntangiblePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["IntangiblePower"].BaseValue,
            Owner.Creature,
            this
        );

    }

    // 升级：无实体层数 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars["IntangiblePower"].UpgradeValueBy(1m);
    }

    public static async Task<CardModel> CreateInHand(Player owner, ICombatState combatState)
    {
        return (await CreateInHand(owner, 1, combatState)).FirstOrDefault();
    }

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, ICombatState combatState)
    {
        if (count == 0 || CombatManager.Instance.IsOverOrEnding)
            return System.Array.Empty<CardModel>();

        List<CardModel> supportCards = new List<CardModel>();
        for (int i = 0; i < count; i++)
        {
            supportCards.Add(combatState.CreateCard<DigitalGhost>(owner));
        }

        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}
