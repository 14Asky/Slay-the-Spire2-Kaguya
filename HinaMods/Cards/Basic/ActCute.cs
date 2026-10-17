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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 卖萌（ActCute）
/// 本地化键：KAGUYA-HINA_MODS_MOON_LEVERAGE.title / .description
/// 1费 · 技能牌 · 基础 · 目标：随机敌人
/// 卡面：获得6点格挡。
///       随机敌人获得1层[gold]虚弱[/gold]1次。
/// 代码实测：Block=6；WeakTimes=1
/// 升级：WeakTimes +1；Block +2
/// </summary>
public sealed class ActCute : HinaModsCard
{
    private const string WeakTimesKey = "WeakTimes";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new DynamicVar(WeakTimesKey, 1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>()
        };
    }

    public ActCute()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.RandomEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        await CommonActions.CardBlock(this, cardPlay);

        int times = base.DynamicVars[WeakTimesKey].IntValue;
        for (int i = 0; i < times; i++)
        {
            var enemies = base.Owner.Creature.CombatState?.HittableEnemies;
            if (enemies == null || enemies.Count == 0)
            {
                break;
            }

            Creature target = base.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                target,
                1m,
                base.Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[WeakTimesKey].UpgradeValueBy(1m);
        base.DynamicVars.Block.UpgradeValueBy(2m);
    }
}
