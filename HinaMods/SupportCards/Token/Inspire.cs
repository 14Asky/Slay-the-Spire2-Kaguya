using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 鼓舞（Inspire）
/// 本地化键：KAGUYA-SUPPORT_STRENGTH.title / .description
/// 3费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：[blue]支援[/blue]：获得{XAmount:diff()}层[gold]力量[/gold]和{VigorAmount:diff()}层[gold]活力[/gold]。
/// 代码实测：CalculationBase=0
/// 升级：无
/// 补充：
///   ⚠ X 取的是「打出瞬间的真实费用」（含所有减费），不是卡面印刷费用。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class Inspire : HinaModsCard
{
    /// <summary>支援专属标签。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    /// <summary>固定 6 层覆甲 + 6 层活力；升级后各 +2（8/8）。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<PlatingPower>(6m),
        new PowerVar<VigorPower>(6m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<PlatingPower>(),
           HoverTipFactory.FromPower<VigorPower>()
        };
    }

    // 构造函数：3费 | 技能牌 | 代币稀有度 | 目标自身
    public Inspire()
        : base(3, CardType.Skill, CardRarity.Token, TargetType.Self)
    { }

    // 打出逻辑：获得覆甲 + 活力
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!cardPlay.IsFirstInSeries)
            return;

        await PowerCmd.Apply<PlatingPower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars[nameof(PlatingPower)].BaseValue,
            base.Owner.Creature,
            this);

        await PowerCmd.Apply<VigorPower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars[nameof(VigorPower)].BaseValue,
            base.Owner.Creature,
            this);
    }

    /// <summary>升级：覆甲 6 → 8；活力 6 → 8。</summary>
    protected override void OnUpgrade()
    {
        base.DynamicVars[nameof(PlatingPower)].UpgradeValueBy(2m);
        base.DynamicVars[nameof(VigorPower)].UpgradeValueBy(2m);
    }

    // ====================== 官方代币生成方法（统一模板） ======================
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
            supportCards.Add(combatState.CreateCard<Inspire>(owner));
        }

        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}
