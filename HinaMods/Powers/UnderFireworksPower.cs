using BaseLib.Abstracts;
using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 在烟花下（UnderFireworksPower）
/// 本地化键：KAGUYA-SUPPORT_FORGE_POWER.title / .description
/// 卡面：每回合开始时，[gold]消耗[/gold]1张牌并获得1；
///       若选择的是[purple]歌声[/purple]牌，则打出该牌并再获得1。
/// </summary>
public sealed class UnderFireworksPower : CustomPowerModel
{
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_forge_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_forge_power.png";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => false;
    public override int DisplayAmount => (int)Amount;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner == null || Owner.Player != player || !Owner.IsAlive)
            return;

        List<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            null,
            null)).ToList();

        if (selected.Count == 0)
            return;

        Flash();

        foreach (CardModel card in selected)
        {
            bool isSong = card is HinaModsCard modCard && modCard.HasEcho;

            await PlayerCmd.GainEnergy(1, player);

            if (isSong)
            {
                await CardPileCmd.Add(card, PileType.Play);
                await CardCmd.AutoPlay(choiceContext, card, null);
                await PlayerCmd.GainEnergy(1, player);
            }
            else
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
        }
    }
}
