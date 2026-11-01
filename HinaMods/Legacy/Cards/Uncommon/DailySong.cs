using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyDailySong : HinaModsCard
{
    public LegacyDailySong()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<LegacySinger>(),
            HoverTipFactory.FromPower<VigorPower>()
        };
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null || player.Creature == null)
            return;

        // 原有活力效果：升级6层，普通4层（完全不变）
        int vigorAmount = IsUpgraded ? 6 : 4;
        await PowerCmd.Apply<VigorPower>(
            choiceContext,
            player.Creature,
            vigorAmount,
            player.Creature,
            this
        );

        // ============== 🔥 仅修改此处：歌声牌生成UI预览并加入抽牌堆（对标MoonLightAria） ==============
        // 直接创建 LegacySinger 卡牌（完全不变）
        CardModel singerCard = CardScope.CreateCard<LegacySinger>(Owner);

        // 升级逻辑（完全不变）
        if (IsUpgraded)
        {
            CardCmd.Upgrade(singerCard);
        }

        // 官方标准：生成卡牌列表 + 加入抽牌堆 + UI预览
        List<CardModel> generatedCards = [singerCard];
        var addResults = await CardPileCmd.AddGeneratedCardsToCombat(
            generatedCards,
            PileType.Draw, // 加入抽牌堆
            Owner,
            CardPilePosition.Random // 位置随机（可改Top为顶部）
        );
        CardCmd.PreviewCardPileAdd(addResults); // 生成UI预览
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}