using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;

namespace Kaguya.Powers;

public sealed class LegacyComputerRoomPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // BaseLib 模板要求提供图标路径（请根据实际资源位置调整）
    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/ComputerRoomPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/ComputerRoomPower.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        // 1. 获得1层过劳
        await PowerCmd.Apply<Overwork>(choiceContext, Owner, 1, Owner, null);

        // 2. 抽1张牌
        await CardPileCmd.Draw(choiceContext, 1, player);

        // 3. 从手牌中选择1张牌消耗
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
        var selected = await CardSelectCmd.FromHand(choiceContext, player, prefs, null, this);
        foreach (var card in selected)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
    }
}