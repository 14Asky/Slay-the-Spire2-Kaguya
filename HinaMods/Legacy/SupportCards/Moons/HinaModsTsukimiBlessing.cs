using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.SupportCards.Moons;

/// <summary>
/// 月之赐福：1费技能
/// 基础：获得1层月见祝福
/// 升级：费用变为0
/// 词条：固有、永恒
/// 效果：打出时若已拥有月见祝福，获得2能量，抽2张牌
/// </summary>
public sealed class LegacyHinaModsTsukimiBlessing : HinaModsCard
{
    // 官方标准动态变量（decimal 规范对齐所有模板）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<LegacyTsukimiBlessingPower>(1m),
        new EnergyVar(2),    // 🔥 修复1：能量变量改为官方 decimal 类型
        new CardsVar(2)       // 抽牌变量（标准写法）
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyTsukimiBlessingPower>(),
            base.EnergyHoverTip
        };
    }

    // 关键词：固有 + 永恒（无修改）
    public override List<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Innate,
        CardKeyword.Eternal
    ];

    // 1费 能力牌 远古稀有 目标自身（无修改）
    public LegacyHinaModsTsukimiBlessing()
        : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self) { }

    // 核心打出逻辑（严格对齐官方模板）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画（无修改）
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 🔥 修复2：补全 PowerCmd.Apply 官方强制5参数（必选上下文）
        await PowerCmd.Apply<LegacyTsukimiBlessingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(LegacyTsukimiBlessingPower)].BaseValue,
            Owner.Creature,
            this
        );

        // 判断：已拥有月见祝福时，触发奖励（逻辑无修改）
        LegacyTsukimiBlessingPower blessing = Owner.Creature.GetPower<LegacyTsukimiBlessingPower>();
        if (blessing != null)
        {
            // 获得能量（对齐Concerted模板）
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            // 抽牌（对齐标准模板）
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
    }

    // 升级效果：1费→0费（无修改，完美生效）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}