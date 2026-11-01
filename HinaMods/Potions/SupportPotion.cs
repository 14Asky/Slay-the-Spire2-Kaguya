using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Potions;

// 注册到你的自定义药水池
/// <summary>
/// 支援药水（SupportPotion）
/// 本地化键：KAGUYA-SUPPORT_POTION.title / .description
/// 卡面：随机将2张[blue]支援牌[/blue]加入你的手牌，这些牌本回合可以免费打出。
/// 代码实测：Cards=2
/// </summary>
[Pool(typeof(HinaModsPotionPool))]
public sealed class SupportPotion : CustomPotionModel
{
    // 稀有度：稀有（可改为 Common 普通 / Uncommon  uncommon）
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    // 仅战斗中使用
    public override PotionUsage Usage => PotionUsage.CombatOnly;

    // 目标：自身
    public override TargetType TargetType => TargetType.Self;

    // 动态变量：固定获取2张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(2) };

    // 药水图标（请替换为你的图片路径）
    public override string CustomPackedImagePath => "res://images/hinamods/Potions/support_potion.png";
    public override string CustomPackedOutlinePath => "res://images/hinamods/Potions/support_potion_outline.png";

    // 药水核心效果：随机获取2张支援牌到手牌
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        // 获取当前拥有者（玩家）
        Player owner = base.Owner;
        if (owner == null) return;

        // ======================
        // ✅ 完全参照你的遗物写法修复
        // ======================
        List<HinaModsCard> allSupportCards = ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c =>
                c.CustomTags != null
                && c.CustomTags.Any()
                && c.CustomTags.Contains(CustomCardTags.SUPPORT))
            .Where(c => HinaModsCard.AllowedInRun(c, owner))
            .ToList();

        // 容错
        if (allSupportCards.Count == 0)
            return;

        // 官方标准：随机获取不重复的卡牌（联机安全）
        IEnumerable<CardModel> selectedCards = CardFactory.GetDistinctForCombat(
            owner,
            allSupportCards.Cast<CardModel>().ToList(),
            DynamicVars.Cards.IntValue, // 固定2张
            owner.RunState.Rng.CombatCardGeneration
        );

        // 生成卡牌到手牌（官方API，联机稳定）
        foreach (CardModel card in selectedCards)
        {
            // 这些牌本回合可以免费打出
            card.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        }
    }
}