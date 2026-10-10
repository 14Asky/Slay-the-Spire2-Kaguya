using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

// 继承你项目的自定义力量基类，完全对标MoonlightBlockPower写法
public sealed class SingerMoonAoEPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/moon_aoe.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/moon_aoe.png";

    public override int DisplayAmount => (int)Amount;
    /// <summary>
    /// ✅ 官方唯一正确的力量数值变化监听（照搬你的MoonlightBlockPower）
    /// 监听：月夜（FortunePower）获得/消耗 时触发全体伤害
    /// </summary>
    // 🔥 核心修复：严格匹配官方方法签名（补全缺失参数+可空类型）
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (Owner == null) return;

        // 核心：检测月夜力量发生变化（获得/消耗 都触发）
        if (power is FortunePower && power.Owner == this.Owner && amount != 0)
        {
            await DealDamageToAllEnemies(choiceContext);
        }
    }

    /// <summary>
    /// ✅ 官方标准：对全体敌人造成伤害（照搬BlackHole）
    /// </summary>
    private async Task DealDamageToAllEnemies(PlayerChoiceContext choiceContext)
    {
        Flash(); // 力量闪光特效
        foreach (var enemy in CombatState.HittableEnemies) { await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), enemy, 3, ValueProp.Unpowered, Owner, null, null); }
    }
}