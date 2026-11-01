using BaseLib.Abstracts;
using Godot;
// 修复：引用正确的BUFF命名空间
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacySupportEnergyCharge : HinaModsCard
{
    public LegacySupportEnergyCharge()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    { }
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacySupportOrbitPower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 🔥 唯一修复：补全官方强制参数 choiceContext
        await PowerCmd.Apply<LegacySupportOrbitPower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        // 2费 → 1费（正确写法）
        EnergyCost.UpgradeBy(-1);
    }
}