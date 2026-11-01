using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

// 月辉协奏 | 0费、技能、自身目标
public sealed class LegacyHinaModsMoonConcerto() : HinaModsCard(0,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    // 官方标准能量动态变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    // 官方能量悬浮提示
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            base.EnergyHoverTip,
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }
    // 核心打出逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 基础效果：所有打出次数都获得能量（无条件）
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

        // 仅【第一次打出】消耗月夜+额外能量
        if (cardPlay.IsFirstInSeries)
        {
            LegacyFortunePower fortune = Owner.Creature.GetPower<LegacyFortunePower>();
            if (fortune != null && fortune.Amount >= 2)
            {
                // 🔥 修复：补全官方强制参数 choiceContext
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    fortune,
                    -2m,
                    Owner.Creature,
                    this);

                // 额外获得1点能量
                await PlayerCmd.GainEnergy(1, Owner);
            }
        }
    }

    // 升级：基础能量 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}