using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 协同准备
/// 1费 技能牌 | 自身目标
/// 生成1张【协同】代币，抽2张牌，获得3层月夜。消耗。
/// 升级：生成2张【协同】代币，固有。
/// </summary>
public sealed class LegacyConcertedPreparation : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1),
        // ✅ 参考模板：添加月夜动态变量定义
        new PowerVar<LegacyFortunePower>(3m)
    ];

    public LegacyConcertedPreparation()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<LegacyConcerted>(),
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 保留原有逻辑不变
        int tokenCount = (int)DynamicVars.Cards.BaseValue;
        for (int i = 0; i < tokenCount; i++)
        {
            CardModel token = CardScope.CreateCard<LegacyConcerted>(Owner);
            await CardPileCmd.Add(token, PileType.Hand);
        }

        await CardPileCmd.Draw(choiceContext, 2, Owner);

        // ✅ 完全对齐模板：调用方式+数值来源 百分百统一
        await PowerCmd.Apply<LegacyFortunePower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars[nameof(LegacyFortunePower)].BaseValue,
            base.Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}