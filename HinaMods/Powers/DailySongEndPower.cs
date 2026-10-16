
using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// DailySongEndPower（DailySongEndPower）
/// 本地化键：KAGUYA-DAILY_SONG_END_POWER.title / .description
/// 卡面：回合结束时，若你本回合未打出过攻击牌，则将随机一张[purple]歌声[/purple]牌加入[gold]抽牌堆[/gold]。
/// </summary>
public sealed class DailySongEndPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => false;
    public override int DisplayAmount => (int)Amount;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/daily_song_end_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/daily_song_end_power.png";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(ctx, side, participants);

        if (side != Owner.Side)
            return;

        Player player = Owner.Player;
        if (player == null)
        {
            await PowerCmd.Remove(this);
            return;
        }

        // 本回合是否打出过攻击牌
        // 用 Owner.Creature.CombatState 而非 CombatManager.Instance.DebugOnlyGetState()：
        // 后者在联机/非战斗上下文可能返回 null，会让 HappenedThisTurn 恒为 false（效果永不触发）。
        MegaCrit.Sts2.Core.Combat.ICombatState? combatState = Owner.CombatState;
        bool playedAttack = combatState != null && CombatManager.Instance.History.CardPlaysFinished
            .Any(e => e.CardPlay.Card.Owner == player
                && e.HappenedThisTurn(combatState)
                && e.CardPlay.Card.Type == CardType.Attack);

        if (!playedAttack)
        {
            List<CardModel> songPool = ModelDb.AllCards
                .OfType<HinaModsCard>()
                .Where(c => c.HasEcho)
                .Cast<CardModel>()
                .ToList();

            if (songPool.Count > 0)
            {
                CardModel card = CardFactory.GetDistinctForCombat(
                    player,
                    songPool,
                    1,
                    player.RunState.Rng.CombatCardGeneration).FirstOrDefault();

                if (card != null)
                {
                    Flash();
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player);
                }
            }
        }

        await PowerCmd.Remove(this);
    }
}
