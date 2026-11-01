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
public sealed class LegacyVigorBurst : HinaModsCard
{
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<VigorPower>(),
            HoverTipFactory.FromPower<LegacyPerformancePower>()
        };
    }

    // 构造函数：1费 | 技能牌 | 代币稀有度 | 目标自身
    public LegacyVigorBurst()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    { }
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
         decimal vigorAmount = 5m;

         await PowerCmd.Apply<VigorPower>(
                choiceContext,
                base.Owner.Creature,
                vigorAmount,
                base.Owner.Creature,
                this
            );

         await PowerCmd.Apply<LegacyPerformancePower>(
                choiceContext,
                base.Owner.Creature,
                1m,
                base.Owner.Creature,
                this
            );
    }

    // 升级逻辑（参考DailySong，仅保留基类调用）
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        AddKeyword(CardKeyword.Retain);
    }

    // ====================== 官方标准代币生成方法（完全照搬模板，一字未改） ======================
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
            supportCards.Add(combatState.CreateCard<LegacyVigorBurst>(owner));
        }

        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}