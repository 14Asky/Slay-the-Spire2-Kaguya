using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Uncommon;

// 0费、技能、自身目标 完全保留
public class LegacyHinaModsMoonBarrier() : HinaModsCard(2,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    // 防御标签 保留

    // 官方标准 BlockVar：基础格挡13
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(13m, ValueProp.Move)];

    // 月夜悬浮提示 保留
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacyFortunePower>(),
        };
    }

    // 核心逻辑：格挡 + 消耗2月夜 → 全体敌人2层虚弱
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 基础格挡逻辑 保留
        await CommonActions.CardBlock(this, cardPlay);

        // 获取月夜BUFF
        LegacyFortunePower fortunePower = Owner.Creature.GetPower<LegacyFortunePower>();

        if (fortunePower != null && fortunePower.Amount >= 2)
        {
            // 🔥 修复：补全 choiceContext 参数
            await PowerCmd.ModifyAmount(
                choiceContext,
                fortunePower,
                -2m,
                Owner.Creature,
                this);

            // 施法动画
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // 核心：对全体敌人施加虚弱（修复参数）
            foreach (var enemy in CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<WeakPower>(
                    choiceContext,
                    enemy,
                    2m,
                    Owner.Creature,
                    this);
            }
        }
    }

    // 升级逻辑：格挡+3
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
