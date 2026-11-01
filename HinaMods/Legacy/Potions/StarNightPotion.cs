using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Potions;

/// <summary>
/// 星夜药水
/// 战斗使用：下一张打出的牌获得重放+消耗
/// </summary>
[Pool(typeof(Kaguya.HinaMods.Potions.HinaModsPotionPool))]
public sealed class LegacyStarNightPotion : CustomPotionModel
{
    // 稀有度
    public override PotionRarity Rarity => PotionRarity.Rare;
    // 仅战斗可用
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    // 目标自身
    public override TargetType TargetType => TargetType.Self;

    // 药水图标
    public override string CustomPackedImagePath => "res://images/hinamods/Potions/star_night_potion.png";
    public override string CustomPackedOutlinePath => "res://images/hinamods/Potions/star_night_potion_outline.png";

    /// <summary>
    /// 使用效果：施加1层星夜之力
    /// </summary>
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        // 完全对标官方复制药水写法
        await PowerCmd.Apply<LegacyStarNightPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, null);
    }
}