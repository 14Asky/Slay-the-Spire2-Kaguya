using BaseLib.Utils;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Relics;

/// <summary>
/// 原光之竹（HinaOriginalLightBambooRelic）
/// 本地化键：KAGUYA-HINA_ORIGINAL_LIGHT_BAMBOO_RELIC.title / .description
/// 卡面：回合结束时，若你没有任何[gold]格挡[/gold]，
///       获得等同于[gold]月夜[/gold]层数的[gold]格挡[/gold]，最多 {MaxBlock} 点。
/// 稀有度：罕见（原为稀有）
/// </summary>
public sealed class HinaOriginalLightBambooRelic : HinaRelics
{
    /// <summary>最多获得的格挡。</summary>
    private const int MaxBlock = 10;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 图标路径
    public override string PackedIconPath => "res://images/hinamods/relics/hina_original_light_bamboo_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/hina_original_light_bamboo_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/hina_original_light_bamboo_relic.png";

    // ====================== 回合结束：没格挡就给格挡 ======================
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        Creature? creature = Owner?.Creature;
        if (creature == null || side != creature.Side)
            return;

        // 身上还有格挡就不触发
        if (creature.Block > 0)
            return;

        decimal moonlight = creature.GetPower<MoonlightPower>()?.Amount ?? 0m;
        int gain = (int)Math.Min(moonlight, MaxBlock);
        if (gain <= 0)
            return;

        await CreatureCmd.GainBlock(creature, gain, ValueProp.Unpowered, null, fast: true);
        Flash();
    }
}
