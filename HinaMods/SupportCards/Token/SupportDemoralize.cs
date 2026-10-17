using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
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
/// 挫敌（SupportDemoralize）
/// 本地化键：KAGUYA-SUPPORT_VULN.title / .description
/// 1费 · 技能牌 · 令牌 · 目标：单个敌人
/// 卡面：[blue]支援[/blue]：给予2层[gold]易伤[/gold]。
/// 代码实测：VulnerablePower=2
/// 升级：费用 -1
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class SupportDemoralize : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
          HoverTipFactory.FromPower<VulnerablePower>()
        };
    }

    public SupportDemoralize() : base(1, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy) { }

    // 🔥 修复1：补全 PowerCmd.Apply 官方必选参数
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!cardPlay.IsFirstInSeries)
            return;

        // 1) 造成伤害
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 2) 给予易伤
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            base.DynamicVars["VulnerablePower"].BaseValue,
            Owner.Creature,
            this
        );
    }

    // 升级：伤害 6 → 9（易伤仍为 2）
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    // ====================== 修复2：修正卡牌生成API错误 ======================
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
            supportCards.Add(combatState.CreateCard<SupportDemoralize>(owner));
        }

        // 🔥 修复3：删除不存在的 addedByPlayer 参数，匹配官方API
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}