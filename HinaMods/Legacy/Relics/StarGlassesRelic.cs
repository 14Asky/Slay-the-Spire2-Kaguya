using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using Kaguya.HinaMods.Legacy.Relics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Relics;

/// <summary>
/// 星星眼镜
/// 专属遗物
/// 当你打出歌声牌时，获得2层活力
/// </summary>
public sealed class LegacyStarGlassesRelic : HinaRelics
{
    // ====================== 基础配置 ======================
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => false;

    // 图标路径（替换为你实际的贴图路径）
    public override string PackedIconPath => "res://images/hinamods/relics/star_glasses_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/star_glasses_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/star_glasses_relic.png";

    // ====================== 核心逻辑：卡牌打出后触发 ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 基础空值校验
        if (Owner == null || cardPlay.Card == null)
            return;

        // 仅自身打出的卡牌生效，排除敌方卡牌
        if (cardPlay.Card.Owner != Owner)
            return;

        // 判断是否为【歌声牌】（匹配歌者专属标签）
        if (cardPlay.Card is not HinaModsCard singerCard
            || !singerCard.CustomTags.Contains(LegacyCustomCardTags.SINGER))
            return;

        // 打出歌声牌 → 获得2层活力（沿用你模组统一的力量施加写法）
        await PowerCmd.Apply<VigorPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            2m,
            Owner.Creature,
            null
        );

        // 遗物闪光特效（统一模组视觉规范）
        Flash();
    }

    // ====================== 固定空实现（对标全系遗物格式） ======================
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        return Task.CompletedTask;
    }
}