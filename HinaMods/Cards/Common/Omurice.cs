using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 蛋包饭（Omurice）
/// 本地化键：KAGUYA-HINA_MODS_OMURICE.title / .description
/// 0费 · 技能牌 · 普通 · 目标：自身 · 关键词：Exhaust
/// 卡面：[gold]月夜1[/gold]：回复2点生命；[gold]月夜3[/gold]：回复4点生命。
/// 代码实测：HealTier1=2；HealTier2=4；MoonCost=3
/// 升级：HealTier1 +1；HealTier2 +1
/// 补充：
///   阶梯判定：只触发满足条件的最高一档，不叠加。
/// </summary>
public sealed class Omurice : HinaModsCard
{
    // 关键词：消耗
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>第一档（月夜1）的回血量。</summary>
    private const decimal HealTier1 = 2m;

    /// <summary>第二档（月夜3）的回血量。</summary>
    private const decimal HealTier2 = 4m;

    /// <summary>第二档的月夜门槛。</summary>
    private const decimal Tier2MoonCost = 3m;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("HealTier1", HealTier1),
        new DynamicVar("HealTier2", HealTier2),
        new PowerVar<MoonlightPower>(MoonCostVarName, Tier2MoonCost)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<MoonlightPower>(),
        };
    }

    public Omurice()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        MoonlightPower moonPower = Owner.Creature.GetPower<MoonlightPower>();
        decimal moon = moonPower?.Amount ?? 0m;

        // 阶梯判定：取满足条件的最高一档，不叠加
        // 月夜 >= 3 → 只回第二档；月夜 == 2 → 只回第一档；否则不回
        if (moon >= Tier2MoonCost)
        {
            await CreatureCmd.Heal(Owner.Creature, (int)DynamicVars["HealTier2"].BaseValue);
        }
        else if (moon >= 2m)
        {
            await CreatureCmd.Heal(Owner.Creature, (int)DynamicVars["HealTier1"].BaseValue);
        }
    }

    /// <summary>升级：两档回血各 +1（2→3、4→5）。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars["HealTier1"].UpgradeValueBy(1m);
        DynamicVars["HealTier2"].UpgradeValueBy(1m);
    }
}
