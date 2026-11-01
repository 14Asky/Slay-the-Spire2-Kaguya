using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Relics;

/// <summary>
/// 八千代的发簪（MoonHeaddressRelic）
/// 本地化键：KAGUYA-MOON_HEADDRESS_RELIC.title / .description
/// 卡面：每当你获得[gold]月夜[/gold]时，额外获得1层[gold]月夜[/gold]。
/// 代码实测：额外 +1 层；含防递归（本遗物额外给出的那 1 层不再触发自己）。
/// </summary>
public sealed class MoonHeaddressRelic : HinaRelics
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://images/hinamods/relics/moon_headdress_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/moon_headdress_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/moon_headdress_relic.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<MoonlightPower>()
    };

    /// <summary>防递归：本遗物额外给出的那 1 层不再触发自己（否则会无限循环）。</summary>
    private bool _granting;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (Owner?.Creature == null) return;
        if (power.Owner != Owner.Creature) return;   // 必须是自己的月夜
        if (power is not MoonlightPower) return;
        if (amount <= 0) return;                     // 只处理「获得」，失去不触发
        if (_granting) return;                       // 防递归

        _granting = true;
        try
        {
            await PowerCmd.Apply<MoonlightPower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                1m,
                Owner.Creature,
                null);

            Flash();
        }
        finally
        {
            _granting = false;
        }
    }
}
