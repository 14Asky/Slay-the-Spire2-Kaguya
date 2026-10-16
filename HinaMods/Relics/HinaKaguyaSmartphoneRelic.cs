using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Relics;

/// <summary>
/// 辉夜的智能手机（HinaKaguyaSmartphoneRelic）
/// 本地化键：KAGUYA-HINA_KAGUYA_SMARTPHONE_RELIC.title / .description
/// 卡面：每场战斗开始时，获得5层[gold]月夜[/gold]。
/// 代码实测：MoonlightPower=5
/// </summary>
public sealed class HinaKaguyaSmartphoneRelic : HinaRelics
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://images/hinamods/relics/hina_kaguya_smartphone_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/hina_kaguya_smartphone_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/hina_kaguya_smartphone_relic.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<MoonlightPower>(5m)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<MoonlightPower>()
    };

    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();

        if (Owner?.Creature == null)
            return;

        await PowerCmd.Apply<MoonlightPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["MoonlightPower"].BaseValue,
            Owner.Creature,
            null);

        Flash();
    }
}
