using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Rare;

// 卡牌类名同步修改
public sealed class LegacyFinalPerformance : HinaModsCard
{
    // 动态变量（对标KnifeTrap标准写法）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(0m)
    ];
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGERCARD };
    // 构造函数：2费 技能牌 稀有 单体目标
    public LegacyFinalPerformance()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 你指定的标签判断逻辑：筛选消耗堆所有歌者牌
        IEnumerable<CardModel> singerCardsInExhaust = PileType.Exhaust
            .GetPile(Owner)
            .Cards
            .Where(c => c is HinaModsCard hinaCard && hinaCard.CustomTags.Contains(LegacyCustomCardTags.SINGER))
            .ToList();

        bool isFirstPlay = true;
        // 复刻KnifeTrap：自动打出所有筛选的卡牌
        foreach (CardModel card in singerCardsInExhaust)
        {
            await CardCmd.AutoPlay(choiceContext, card, cardPlay.Target, AutoPlayType.Default, skipXCapture: false, !isFirstPlay);
            isFirstPlay = false;
        }
    }

    // 升级：2费 → 1费
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}