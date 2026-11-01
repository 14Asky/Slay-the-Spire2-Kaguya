using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
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
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyMoonlightSlash : HinaModsCard
{
    // 完全复刻官方Ftl：常量定义键名
    private const string _playMaxKey = "PlayMax";

    // 完全复刻官方Ftl：动态变量（伤害+最大触发次数+月夜层数）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new IntVar(_playMaxKey, 3m),    // 本回合前3张触发
        new PowerVar<LegacyFortunePower>(3m)  // 月夜Buff层数
    };

	// 沿用你的模组配置：月夜标签 + Buff悬浮提示

	protected override IEnumerable<IHoverTip> GetCustomHoverTips()
	{
		return new IHoverTip[]
		{
			HoverTipFactory.FromPower<LegacyFortunePower>()
		};
	}

	// 官方标准构造函数
	public LegacyMoonlightSlash()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 核心：复刻Ftl官方判断逻辑（本回合已出牌数 < 最大触发数）
    private bool CanGetMoonlight
    {
        get
        {
            // 官方原版统计：本回合、自己打出的卡牌总数
            int playedCount = CombatManager.Instance.History.CardPlaysFinished
                .Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner);

            // 小于3 → 可以获得月夜
            return playedCount < DynamicVars[_playMaxKey].IntValue;
        }
    }

    // 复刻Ftl官方OnPlay逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 官方标准单体攻击
        AttackCommand attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash");

        await attack.Execute(choiceContext);

        // 官方条件判断：满足则获得月夜
        if (CanGetMoonlight && cardPlay.IsFirstInSeries)
        {
            decimal moonAmount = DynamicVars[nameof(LegacyFortunePower)].BaseValue;

            // 🔥 唯一修复：补全官方强制参数 choiceContext
            await PowerCmd.Apply<LegacyFortunePower>(
                choiceContext,
                Owner.Creature,
                moonAmount,
                Owner.Creature,
                this);
        }
    }

    // 复刻Ftl官方升级逻辑
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);      // 8→12
        DynamicVars[nameof(LegacyFortunePower)].UpgradeValueBy(2m); // 3→5
    }
}