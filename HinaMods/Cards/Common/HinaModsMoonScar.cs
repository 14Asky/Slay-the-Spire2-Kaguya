using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 月痕
/// 1费 攻击牌 | 单体目标
/// 造成7点伤害。消耗1层月夜，再次造成7点伤害。
/// 升级：伤害变为8点。
/// </summary>
public sealed class HinaModsMoonScar : HinaModsCard
{
    

    public HinaModsMoonScar()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move)
    ];

	protected override IEnumerable<IHoverTip> GetCustomHoverTips()
	{
		return new IHoverTip[]
		{
			HoverTipFactory.FromPower<FortunePower>()
		};
	}


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay, nameof(cardPlay));
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);

        FortunePower fortunePower = Owner.Creature.GetPower<FortunePower>();
        if (fortunePower != null && fortunePower.Amount >= 1)
        {
            // 修复：添加 choiceContext 参数
            await PowerCmd.ModifyAmount(
                choiceContext,
                fortunePower,
                -1m,
                Owner.Creature,
                this);
            await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}