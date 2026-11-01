using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;          // Cmd.Wait
using MegaCrit.Sts2.Core.Nodes.Rooms;     // NCombatRoom
using MegaCrit.Sts2.Core.Nodes.Vfx;       // NGrandFinaleVfx
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Powers;

public sealed class LegacyGoodEndingPower : CustomPowerModel
{
    private const int RequiredTurns = 12;
    private int _turnCounter = 0;
    private bool _hasTriggered = false;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override int DisplayAmount => _turnCounter;

    public override string CustomPackedIconPath => "res://images/powers/good_ending_power.png";
    public override string CustomBigIconPath => "res://images/powers/good_ending_power.png";

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (_hasTriggered) return;

        _turnCounter++;
        InvokeDisplayAmountChanged();

        if (_turnCounter >= RequiredTurns)
        {
            _hasTriggered = true;

            // 1. 播放 GrandFinale 特效（与 HappyEnd 相同）
            NGrandFinaleVfx vfx = NGrandFinaleVfx.Create(Owner);
            if (vfx != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
                await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
            }

            // 2. 完全模拟 win 指令：移除所有敌人能力并杀死，最后检查胜利
            var enemies = Owner.CombatState.Enemies.ToList();
            foreach (var enemy in enemies)
            {
                enemy.RemoveAllPowersInternalExcept(); // 内部清除，绕过转阶段保护
                await CreatureCmd.Kill(enemy);
            }

            await CombatManager.Instance.CheckWinCondition();
            Flash();
        }
    }
}