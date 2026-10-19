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
/// 2费 · 技能牌 · 普通 · 目标：自身 · 关键词：Ethereal（升级后移除）
/// 卡面：获得15点格挡，然后获得2层[gold]脆弱[/gold]。
/// 代码实测：Block=15；FrailPower=2
/// 升级：Block 15 → 18
/// </summary>
public sealed class Loneliness : HinaModsCard
{
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(15m, ValueProp.Move),
        new PowerVar<FrailPower>(2m)
    ];

    public Loneliness()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先结算格挡，再给自己挂脆弱（顺序反了格挡会先被脆弱削减）
        await CommonActions.CardBlock(this, cardPlay);

        await PowerCmd.Apply<FrailPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["FrailPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
