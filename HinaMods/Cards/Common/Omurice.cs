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
/// 本地化键：KAGUYA-OMURICE.title / .description
/// 0费 · 技能牌 · 普通 · 目标：自身 · 关键词：消耗
/// 卡面：获得4层[gold]月夜[/gold]；[gold]月夜2[/gold]：回复4点生命。
/// 代码实测：MoonlightPower=4；MoonCost=2；Heal=4
/// 升级：月夜消耗 -1（2→1）
/// 说明：先获得月夜，再消耗月夜回复生命；月夜不足则不回复。
/// </summary>
public sealed class Omurice : HinaModsCard
{
    // 关键词：消耗
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HealVar(3m),                    // 回复 3 点生命（变量名 Heal）
        new PowerVar<MoonlightPower>(3m)    // 获得 3 层月夜（变量名 MoonlightPower）
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
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 1) 回复生命
        await CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars.Heal.IntValue);

        // 2) 获得月夜
        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars[nameof(MoonlightPower)].BaseValue,
            base.Owner.Creature,
            this);
    }

    /// <summary>升级：回血 3 → 4；月夜 3 → 4。</summary>
    protected override void OnUpgrade()
    {
        base.DynamicVars.Heal.UpgradeValueBy(1m);
        base.DynamicVars[nameof(MoonlightPower)].UpgradeValueBy(1m);
    }
}
