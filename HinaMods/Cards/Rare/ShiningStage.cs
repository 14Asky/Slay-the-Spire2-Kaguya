using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 闪耀舞台（ShiningStage）
/// 本地化键：KAGUYA-SINGER_CONVERT.title / .description
/// 2费 · 能力牌 · 稀有 · 目标：自身
/// 卡面：回合结束时，获得等同于你本回合打出卡牌数的[gold]活力[/gold]。
/// 升级：费用 -1
/// </summary>
public class ShiningStage() : HinaModsCard(2,
    CardType.Power, CardRarity.Rare,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<ShiningStagePower>(),
            HoverTipFactory.FromPower<VigorPower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 获得1层闪耀舞台
        await PowerCmd.Apply<ShiningStagePower>(
            choiceContext,
            player.Creature,
            1m,
            player.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
