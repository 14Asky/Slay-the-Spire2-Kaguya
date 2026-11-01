using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

[Pool(typeof(StatusCardPool))]
public sealed class LegacyExpectation : HinaModsCard
{
    // 状态牌固定配置
    public override int MaxUpgradeLevel => 0;
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override bool HasTurnEndInHandEffect => false;
    protected override IEnumerable<string> ExtraRunAssetPaths => Enumerable.Empty<string>();

    // 动态变量：抽1张、获得2层月夜
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(1),
        new PowerVar<LegacyFortunePower>(3m)
    };

    // 官方状态牌构造
    public LegacyExpectation()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.Self)
    { }

    // 抽到自动触发
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        if (card == this && Owner != null)
        {
            await Cmd.Wait(0.25f);

            // 抽1张牌
            int drawCnt = (int)DynamicVars.Cards.BaseValue;
            await CardPileCmd.Draw(choiceContext, drawCnt, Owner);

            // 获得3层月夜
            decimal fortuneAmt = DynamicVars[nameof(LegacyFortunePower)].BaseValue;
            await PowerCmd.Apply<LegacyFortunePower>(
                choiceContext,
                Owner.Creature,
                fortuneAmt,
                Owner.Creature,
                this);
        }
    }

    // 无法打出
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }

    // 不可升级
    protected override void OnUpgrade()
    {
    }
}