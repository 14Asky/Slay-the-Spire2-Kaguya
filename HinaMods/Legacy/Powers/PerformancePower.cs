using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

/// <summary>
/// 演奏
/// 每层效果：下一张攻击牌打出时，返还你消耗的所有活力
/// 可叠加，永久生效直到层数被消耗完
/// </summary>
public sealed class LegacyPerformancePower : CustomPowerModel
{
    // 记录攻击前的活力值
    private decimal _vigorBeforePlay;

    // ====================== 完全对齐 LegacyTsukimiContractPower 配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加层数
    public override bool AllowNegative => false;

    // 图标、层数显示（与参考代码格式一致）
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/performance_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/performance_power.png";
    public override int DisplayAmount => (int)Amount; // 显示当前剩余生效次数

    // ====================== 核心逻辑：攻击前记录当前活力值 ======================
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay);

        // 官方标准判断：存活+玩家+自己打出的牌
        if (Owner == null || !Owner.IsAlive || !Owner.IsPlayer)
        {
            _vigorBeforePlay = 0;
            return;
        }
        if (cardPlay.Card.Owner != Owner.Player)
        {
            _vigorBeforePlay = 0;
            return;
        }

        // 仅对攻击牌生效，且还有剩余层数
        if (cardPlay.Card.Type != CardType.Attack || Amount <= 0)
        {
            _vigorBeforePlay = 0;
            return;
        }

        // ✅ 修复：通过获取当前活力层数记录攻击前状态
        var vigorPower = Owner.GetPower<VigorPower>();
        _vigorBeforePlay = vigorPower?.Amount ?? 0m;
    }

    // ====================== 核心逻辑：攻击后计算消耗并返还 ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 官方标准判断：存活+玩家+自己打出的牌
        if (Owner == null || !Owner.IsAlive || !Owner.IsPlayer)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;

        // 仅对攻击牌生效，且有记录的活力值
        if (cardPlay.Card.Type != CardType.Attack || _vigorBeforePlay <= 0 || Amount <= 0)
            return;

        // ✅ 修复：通过攻击前后差值计算实际消耗的活力
        var currentVigorPower = Owner.GetPower<VigorPower>();
        decimal currentVigor = currentVigorPower?.Amount ?? 0m;
        decimal vigorConsumed = _vigorBeforePlay - currentVigor;

        // 只有实际消耗了活力才返还
        if (vigorConsumed > 0)
        {
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner,
                vigorConsumed,
                Owner,
                null
            );
        }

        // 消耗1层buff
        await PowerCmd.ModifyAmount(choiceContext, this, -1m, Owner, null);
        // 清空本次记录
        _vigorBeforePlay = 0;
    }
}