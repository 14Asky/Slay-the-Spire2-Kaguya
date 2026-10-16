using BaseLib.Abstracts;
using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 月之赐福（EndingPower）
/// 本地化键：KAGUYA-ENDING.title / .description
/// 卡面：你每打出一张[blue]支援[/blue]牌就获得{MoonPerSupport:diff()}层[gold]月夜[/gold]；
///       每当你累积打出{SongThreshold:diff()}张[purple]歌声[/purple]牌时，就随机获得{SupportPerTrigger:diff()}张[blue]支援[/blue]牌。
/// </summary>
public sealed class EndingPower : CustomPowerModel
{
    /// <summary>累积多少张歌声牌触发一次。</summary>
    private const int SongThreshold = 3;

    /// <summary>每次触发获得的支援牌张数。</summary>
    private const int SupportPerTrigger = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => false;
    public override int DisplayAmount => (int)Amount;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/tsukimi_blessing.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/tsukimi_blessing.png";

    /// <summary>本场战斗已累积打出的歌声牌张数。</summary>
    private int _songPlayed;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        if (Owner == null || !Owner.IsAlive)
            return;
        if (cardPlay.Card is not HinaModsCard played)
            return;
        if (cardPlay.Player == null || cardPlay.Player != Owner.Player)
            return;

        // ① 每打出 1 张支援牌 → 获得 Amount 层月夜
        if (played.CustomTags != null && played.CustomTags.Contains(CustomCardTags.SUPPORT))
        {
            Flash();
            await PowerCmd.Apply<MoonlightPower>(choiceContext, Owner, Amount, Owner, null);
        }

        // ② 每累积打出 3 张歌声牌 → 随机获得 1 张支援牌
        if (played.HasEcho)
        {
            _songPlayed++;
            if (_songPlayed < SongThreshold)
                return;

            _songPlayed -= SongThreshold;

            Player player = Owner.Player;
            if (player == null)
                return;

            List<CardModel> pool = ModelDb.AllCards
                .OfType<HinaModsCard>()
                .Where(c => c.CustomTags != null && c.CustomTags.Contains(CustomCardTags.SUPPORT))
                .Cast<CardModel>()
                .ToList();

            if (pool.Count == 0)
                return;

            List<CardModel> cards = CardFactory.GetDistinctForCombat(
                player,
                pool,
                SupportPerTrigger,
                player.RunState.Rng.CombatCardGeneration).ToList();

            Flash();
            foreach (CardModel card in cards)
            {
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
            }
        }
    }
}
