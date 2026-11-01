using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.SupportCards.Common;

public sealed class LegacyHinaModsWinStrike : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(30m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<LegacySupportStrike>(),
        };
    }

    public LegacyHinaModsWinStrike()
        : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        // 目标合法性校验（核心防护，防止崩溃）
        if (cardPlay.Target is not Creature target || target.IsDead)
            return;

        // 获取基础数值
        decimal damageValue = DynamicVars.Damage.BaseValue;
        Creature attacker = Owner.Creature;

        IEnumerable<DamageResult> damageResults = await CreatureCmd.Damage(
            choiceContext: choiceContext, targets: new List<Creature> { target }, amount: damageValue, props: ValueProp.Move, cardSource: this, dealer: attacker, cardPlay: null);

        // ✅ 完全还原原逻辑：判断是否击杀目标
        bool wasKilled = damageResults.Any(result => result != null && result.WasTargetKilled);

        // 击杀目标 → 获得3张支援打击（原版功能100%保留）
        if (wasKilled)
        {
            for (int i = 0; i < 3; i++)
            {
                CardModel newCard = CombatState.CreateCard<LegacySupportStrike>(Owner);
                await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
            }
        }
    }

    // 升级效果完全保留：伤害+8
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
    }
}