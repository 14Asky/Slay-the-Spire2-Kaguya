using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 月光重甲守护
/// 1费 技能牌 | 消耗
/// 基础：将1张【支援·重甲格挡】加入手牌
/// 升级：加入手牌的支援牌直接升级
/// 额外：消耗2层月夜，将1张升级【支援·重甲格挡】置入抽牌堆顶部
/// </summary>
public sealed class LegacyMoonLightAria : HinaModsCard
{
    // 无任何数值变量
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<LegacySupportHeavyBlock>(),
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    // 构造：1费 技能牌 白卡 自身目标
    public LegacyMoonLightAria()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ========== 核心效果：将支援格挡加入手牌 ==========
        CardModel newHandCard = CardScope.CreateCard<LegacySupportHeavyBlock>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(newHandCard);
        }
        await CardPileCmd.Add(newHandCard, PileType.Hand);

        // ========== 新增月夜效果：消耗2层月夜，加入抽牌堆顶部 ==========
        LegacyFortunePower fortunePower = Owner.Creature.GetPower<LegacyFortunePower>();
        if (fortunePower != null && fortunePower.Amount >= 2)
        {
            // 消耗2层月夜
            await PowerCmd.ModifyAmount(
                choiceContext,
                fortunePower,
                -2m,
                Owner.Creature,
                this);

            // 生成卡牌
            CardModel newDrawCard = CardScope.CreateCard<LegacySupportHeavyBlock>(Owner);

            // 🔥 关键修复：只有本卡牌【已升级】，才给抽牌堆的牌升级
            if (IsUpgraded)
            {
                CardCmd.Upgrade(newDrawCard);
            }

            // 官方API：加入抽牌堆顶部 + UI预览
            List<CardModel> generatedCards = [newDrawCard];
            var addResults = await CardPileCmd.AddGeneratedCardsToCombat(
                generatedCards,
                PileType.Draw,
                Owner,
                CardPilePosition.Top // 顶部，更符合描述
            );
            CardCmd.PreviewCardPileAdd(addResults);
        }
    }

    protected override void OnUpgrade()
    {
    }
}