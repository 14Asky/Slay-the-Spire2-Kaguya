using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
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
public sealed class LegacyLoneliness : HinaModsCard
{
    // 状态牌标准配置（无法打出、不可升级）
    public override int MaxUpgradeLevel => 0;
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override bool HasTurnEndInHandEffect => false;
    protected override IEnumerable<string> ExtraRunAssetPaths => Enumerable.Empty<string>();

    // 动态变量：修改为 8 点格挡
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, ValueProp.Move)];

    // 官方状态牌构造函数
    public LegacyLoneliness()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.Self)
    { }

    // 抽到自动触发：直接获得8格挡，无任何其他效果
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        if (card == this && Owner != null)
        {
            await Cmd.Wait(0.25f);
            // 核心：抽到直接获得格挡
            await CommonActions.CardBlock(this, null);
        }
    }

    // 无法打出，空实现
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }

    // 无升级效果
    protected override void OnUpgrade()
    {
    }
}