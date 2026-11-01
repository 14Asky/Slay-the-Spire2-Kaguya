using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using Kaguya.HinaMods.Legacy.Relics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Relics;

/// <summary>
/// 辉夜的耳机
/// 普通遗物
/// 战斗开始时：随机获得1张歌者牌加入手牌顶部
/// </summary>
public sealed class LegacyHinaPhonographRelic : HinaRelics
{
    // ====================== 基础配置（完全不变） ======================
    public override RelicRarity Rarity => RelicRarity.Common;
    public override bool ShowCounter => false;

    // 图标路径（完全不变）
    public override string PackedIconPath => "res://images/hinamods/relics/hina_phonograph_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/hina_phonograph_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/hina_phonograph_relic.png";

    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();

        // 标准空检查（完全不变）
        if (Owner?.Creature == null || Owner.RunState == null)
            return;

        // 筛选歌者牌（完全不变）
        List<HinaModsCard> allSingerCards = ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c =>
                c.CustomTags != null
                && c.CustomTags.Any()
                && c.CustomTags.Contains(LegacyCustomCardTags.SINGER))
            .ToList();

        // 容错（完全不变）
        if (allSingerCards.Count == 0)
            return;

        // 🔥 修复：自动随机获得1张歌者牌（无玩家交互，彻底解决崩溃）
        // 使用官方战斗同步随机数，保证联机一致
        List<CardModel> selectedCards = CardFactory
            .GetDistinctForCombat(Owner, allSingerCards.Cast<CardModel>().ToList(), 1, Owner.RunState.Rng.CombatCardGeneration)
            .ToList();

        CardModel chosenCard = selectedCards.FirstOrDefault();
        if (chosenCard != null)
        {
            // 加入手牌顶部（完全不变）
            await CardPileCmd.AddGeneratedCardToCombat(
                chosenCard,
                PileType.Hand,
                Owner,
                CardPilePosition.Top);

            Flash(); // 遗物闪烁（完全不变）
        }
    }

    // ====================== 官方标准空实现（完全不变） ======================
    public override async Task AfterCombatEnd(CombatRoom _)
    {
        await Task.CompletedTask;
    }
}