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
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.SupportCards.Common;

/// <summary>
/// 升级后：获得2张支援打击
/// </summary>
public sealed class GenerateSupportStrike : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move)
    };
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<SupportStrike>()
        };
    }

    public GenerateSupportStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 造成伤害（不变）
        await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);

        // 2. 获取支援打击卡牌（不变）
        CardModel supportCard = ModelDb.Card<SupportStrike>();
        if (supportCard == null) return;

        // ==============================================
        // 🔥 核心修改：升级获得2张，未升级获得1张
        // ==============================================
        int addCount = IsUpgraded ? 2 : 1;
        for (int i = 0; i < addCount; i++)
        {
            CardModel newCard = CombatState.CreateCard(supportCard, Owner);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}