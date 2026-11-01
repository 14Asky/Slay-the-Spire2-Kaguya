using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyHinaModsBirth : HinaModsCard
{
    // 消耗关键词
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 动态变量：8点格挡
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(9m, ValueProp.Move)
    ];

    // 构造：0费 技能 白卡 目标自身
    public LegacyHinaModsBirth()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 官方标准格挡写法，无参数错误
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block.BaseValue,
            ValueProp.Move,
            cardPlay
        );
    }

    // 升级：10 → 14 格挡
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}