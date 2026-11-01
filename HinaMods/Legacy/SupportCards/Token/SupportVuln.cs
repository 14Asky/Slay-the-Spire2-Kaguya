using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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

namespace Kaguya.HinaMods.Legacy.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class LegacySupportVuln : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORT };
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m)];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
          HoverTipFactory.FromPower<VulnerablePower>()
        };
    }

    public LegacySupportVuln() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy) { }

    // 🔥 修复1：补全 PowerCmd.Apply 官方必选参数
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.IsFirstInSeries)
        {
            // 官方固定5参数：上下文、目标、数值、施加者、卡牌来源
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                cardPlay.Target,
                base.DynamicVars["VulnerablePower"].BaseValue,
                Owner.Creature,
                this
            );
        }
        // 同步修复支援技能施加
        await PowerCmd.Apply<LegacySupportSkillPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => base.DynamicVars["VulnerablePower"].UpgradeValueBy(1m);

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
            supportCards.Add(combatState.CreateCard<LegacySupportVuln>(owner));
        }

        // 🔥 修复3：删除不存在的 addedByPlayer 参数，匹配官方API
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}