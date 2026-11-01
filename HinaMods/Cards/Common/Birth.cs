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

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 诞生（Birth）
/// 本地化键：KAGUYA-HINA_MODS_BIRTH.title / .description
/// 0费 · 技能牌 · 普通 · 目标：自身 · 关键词：Exhaust
/// 卡面：获得9点格挡。
/// 代码实测：Block=9
/// 升级：Block +3
/// </summary>
public sealed class Birth : HinaModsCard
{
    // 消耗关键词
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 动态变量：8点格挡
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(9m, ValueProp.Move)
    ];

    // 构造：0费 技能 白卡 目标自身
    public Birth()
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

    // 升级：格挡 +3 → 12 格挡
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}