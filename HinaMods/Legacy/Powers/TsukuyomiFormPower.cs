using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Powers;

public sealed class LegacyTsukuyomiFormPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // 自定义图标
    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/TsukuyomiFormPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/TsukuyomiFormPower.png";

    // 在每回合开始（玩家侧）时触发，替代 BeforeHandDraw
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        var player = Owner.Player;
        if (player == null) return;

        // 移除现实（Reality）
        var reality = Owner.GetPower<Reality>();
        if (reality != null)
        {
            await PowerCmd.Remove(reality);
        }

        // 移除月读（Tsukuyomi）
        var tsukuyomi = Owner.GetPower<Tsukuyomi>();
        if (tsukuyomi != null)
        {
            await PowerCmd.Remove(tsukuyomi);
        }

        // 进入月读（需要提供 PlayerChoiceContext，这里使用非关键上下文的简单方式，若需要上下文可使用新创建或传入）
        // 注：如果 Apply 需要 choiceContext，可以创建一个 ThrowingPlayerChoiceContext，或从 combatState 获取当前上下文（若无严格要求）
        await PowerCmd.Apply<Tsukuyomi>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);

        Flash();
    }
}
