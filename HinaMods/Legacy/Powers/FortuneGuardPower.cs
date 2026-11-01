using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacyFortuneGuardPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/fortune_guard_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/fortune_guard_power.png";
    public override int DisplayAmount => (int)Amount;

    // 仅记录【自己】未被格挡的伤害
    private int _unblockedDamageTaken;

    // 官方伤害监听 → 仅检测自己，其他人完全忽略
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature _, CardModel __)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, _, __);

        if (target != Owner)
            return;

        _unblockedDamageTaken += result.UnblockedDamage;
    }

    // 回合开始触发 → 仅自己生效
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        // 🔥 仅自己的回合、自己是持有者才生效
        if (Owner != player.Creature || !Owner.IsAlive)
            return;

        // 自己未受到穿透伤害 → 获得5月夜
        if (_unblockedDamageTaken <= 0)
        {
            // 🔥 唯一修复：补上 choiceContext，完全匹配官方API
            await PowerCmd.Apply<LegacyFortunePower>(choiceContext, Owner, 5m, Owner, null);
        }

        // 触发后重置计数器 + 移除BUFF
        _unblockedDamageTaken = 0;
        await PowerCmd.Remove(this);
    }
}