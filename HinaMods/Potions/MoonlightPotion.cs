using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Potions;

/// <summary>
/// 月亮药水（MoonPotion）
/// 本地化键：KAGUYA-MOON_POTION.title / .description
/// 卡面：获得6层[gold]月夜[/gold]。
/// 代码实测：MoonlightPower=6
/// </summary>
[Pool(typeof(HinaModsPotionPool))]
public sealed class MoonPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<MoonlightPower>(6m)
    };

    public override string CustomPackedImagePath => "res://images/hinamods/Potions/moon_potion.png";
    public override string CustomPackedOutlinePath => "res://images/hinamods/Potions/moon_potion_outline.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MoonlightPower"].BaseValue,
            Owner.Creature,
            null);
    }
}
