using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 寂寞（Loneliness）
/// 本地化键：KAGUYA-LONELINESS.title / .description
/// 2费 · 技能牌 · 普通 · 目标：自身 · 关键词：Ethereal
/// 卡面：获得13点格挡；本回合不再能获得格挡。
/// 代码实测：Block=13
/// 升级：Block +3；移除关键词 Ethereal
/// </summary>
public sealed class Loneliness : HinaModsCard
{
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(13m, ValueProp.Move)
    ];

    public Loneliness()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先结算本身的格挡，再封锁本回合后续的格挡获取
        await CommonActions.CardBlock(this, cardPlay);

        await PowerCmd.Apply<NoBlockPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
