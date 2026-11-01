using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Potions;

[Pool(typeof(Kaguya.HinaMods.Potions.HinaModsPotionPool))]
public sealed class LegacyMoonPotion : CustomPotionModel
{
    // 官方标准配置
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    // 🔥 完全对标你的松饼卡牌，标准动态变量写法（无报错）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<LegacyFortunePower>(3m)
    };

    // 图标路径
    public override string CustomPackedImagePath => "res://images/hinamods/Potions/moon_potion.png";
    public override string CustomPackedOutlinePath => "res://images/hinamods/Potions/moon_potion_outline.png";

    // 🔥 极简官方写法，无参数前缀，对标参考代码
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        // 核心：读取动态变量，施加月夜（和你的卡牌完全一致）
        await PowerCmd.Apply<LegacyFortunePower>(
            choiceContext,
            Owner.Creature,
            base.DynamicVars["LegacyFortunePower"].BaseValue,
            Owner.Creature,
            null
        );
    }
}