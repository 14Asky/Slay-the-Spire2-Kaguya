using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

// 自定义防御卡牌：继承模组基础卡牌，费用1、技能类型、基础稀有度、目标为自身
public class LegacyHinaModsBlock() : HinaModsCard(1,
    CardType.Skill, CardRarity.Basic,
    TargetType.Self)
{
    // 卡牌核心标签：标记为【防御】标签
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    // 卡牌基础数值：获得5点格挡
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];

    // 打出卡牌时执行：为自身获得格挡值
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }

    // 卡牌升级效果：格挡值提升2点
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}