using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using Kaguya.HinaMods.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 招架 | 1费技能
/// 获得6点格挡，将2张【支援格挡】加入手牌
/// 升级：获得9点格挡，将3张【支援格挡】加入手牌
/// </summary>
public sealed class ParryParry : HinaModsCard
{
    // 防御标签（标准格挡卡配置）

    // 动态变量：基础5点格挡
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(9m, ValueProp.Move)
    ];

    // 构造函数：2费 技能 普通卡 目标自身
    public ParryParry()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<SupportBlock>()
        };
    }

    // 核心打出逻辑（完全对标 GenerateSupportStrike）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得基础格挡
        await CommonActions.CardBlock(this, cardPlay);

        // 2. 基础生成1张支援格挡，升级后生成2张
        int tokenCount = IsUpgraded ? 2 : 1;

        // 3. 循环生成支援格挡代币并加入手牌
        for (int i = 0; i < tokenCount; i++)
        {
            CardModel supportBlock = CardScope.CreateCard<SupportBlock>(Owner);
            await CardPileCmd.Add(supportBlock, PileType.Hand);
        }
    }

    // 升级效果：格挡 6→9（+3），支援格挡数量 2→3
    protected override void OnUpgrade()
    {
    }
}
