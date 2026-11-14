using Godot;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Cards
{
    [CustomID("KAGUYA-JOY")]
    public sealed class Joy : CardModel
    {
        public override string PortraitPath => "res://images/packed/card_portraits/status/joy.png";

        // 无法升级
        public override int MaxUpgradeLevel => 0;

        // 卡牌关键词：不可打出 + 虚无（回合结束时消耗）
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
        {
            CardKeyword.Unplayable,
        };

        // 自定义标签（与劳累等一致，用于辉夜姬状态卡）
        public override IEnumerable<CardTag> Tags => new[] { (CardTag)1000 };

        // 构造函数：-1费，状态牌，稀有度状态，无目标
        public Joy() : base(-1, CardType.Status, CardRarity.Status, TargetType.None) { }

        // 抽到这张牌时触发
        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (card != this) return;

            // 抽2张牌
            await CardPileCmd.Draw(choiceContext, 2, Owner);
        }
    }
}
