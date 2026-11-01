// 月夜力量命名空间
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

// 完全参考 Hellraiser 官方格式 | 2费 攻击牌 稀有 全体目标
public sealed class LegacyGameLife : HinaModsCard
{
    // 构造函数：2费（升级不改变费用）
    public LegacyGameLife()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    // 严格按官方格式：BlockVar 双参数构造，无报错
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
        new DamageVar(5m, ValueProp.Move),
        new PowerVar<LegacyFortunePower>(4m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    // 抽牌自动打出（全保留）
    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(ctx, card, fromHandDraw);
        if (card == this && Owner != null)
        {
            await Cmd.Wait(0.25f);
            SetToFreeThisTurn();
            await CardCmd.AutoPlay(ctx, this, null);
        }
    }

    // 核心效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 未升级失去1能量（保留）
        if (!IsUpgraded)
        {
            await PlayerCmd.LoseEnergy(1, Owner);
        }

        // 格挡（动态变量）
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, cardPlay, fast: true);

        // 伤害（动态变量）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        // 🔥 修复：补全官方参数 + 使用动态变量（和你定义的3/4层保持一致）
        await PowerCmd.Apply<LegacyFortunePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(LegacyFortunePower)].BaseValue,
            Owner.Creature,
            this);
    }

    // 升级效果
    protected override void OnUpgrade()
    {
        // 伤害+2
        DynamicVars.Damage.UpgradeValueBy(2m);
        // 格挡+2
        DynamicVars.Block.UpgradeValueBy(2m);
        // 月夜层数+1 → 3→4
        DynamicVars[nameof(LegacyFortunePower)].UpgradeValueBy(2m);
    }
}