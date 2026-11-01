//using BaseLib.Abstracts;
//using BaseLib.Extensions;
//using BaseLib.Utils;
//using Godot;
//using MegaCrit.Sts2.Core.Combat;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Creatures;
//using MegaCrit.Sts2.Core.Entities.Players;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.HoverTips;
//using MegaCrit.Sts2.Core.Localization;
//using MegaCrit.Sts2.Core.Models;
//using Kaguya.HinaMods;
//using Kaguya.HinaMods.Character;
//using Kaguya.HinaMods.Extensions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Legacy.Cards;

//public abstract class LegacyMoonsCard(int cost, CardType type, CardRarity rarity, TargetType target) :
//    CustomCardModel(cost, type, rarity, target)
//{
//    public virtual HashSet<string> CustomTags { get; } = new HashSet<string>();

//    public LegacyMoonsCard() : this(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
//    {
//    }

//    // 修复类型转换错误 + 仅支援卡添加消耗+虚无
//    public override List<CardKeyword> CanonicalKeywords =>
//        CustomTags.Contains(LegacyCustomCardTags.SUPPORT)
//        // 支援卡：直接返回列表（你要的格式）
//        ? [
//            CardKeyword.Exhaust,
//        CardKeyword.Ethereal
//        ]
//        // 普通卡：将基类转为List，解决编译报错
//        : base.CanonicalKeywords.ToList();

//    protected override IEnumerable<IHoverTip> ExtraHoverTips
//    {
//        get
//        {
//            // 1. 保留游戏原生的基础提示
//            foreach (var tip in base.ExtraHoverTips)
//                yield return tip;

//            if (CustomTags.Contains(LegacyCustomCardTags.MOONYEARS))
//            {
//                yield return new HoverTip(
//                    new LocString("card_keywords", "MOONYEARS.title"),
//                    new LocString("card_keywords", "MOONYEARS.description")
//                );
//            }
//        }
//    }

//    // ================================================================================
//    // 卡牌图片路径（原生逻辑，无修改）
//    // ================================================================================
//    public override string CustomPortraitPath
//    {
//        get
//        {
//            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
//            return ResourceLoader.Exists(path) ? path : "card.png".BigCardImagePath();
//        }
//    }

//    public override string PortraitPath
//    {
//        get
//        {
//            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
//            return ResourceLoader.Exists(path) ? path : "card.png".CardImagePath();
//        }
//    }

//    public override string BetaPortraitPath
//    {
//        get
//        {
//            var path = $"Beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
//            return ResourceLoader.Exists(path) ? path : "Beta/card.png".CardImagePath();
//        }
//    }
//}
