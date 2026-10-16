using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Ancient;

/// <summary>
/// 月之赐福（MoonBlessing）
/// 本地化键：KAGUYA-ENDING.title / .description
/// 2费 · 能力牌 · 先古 · 目标：自身
/// 卡面：你每打出一张[blue]支援[/blue]牌就获得1层[gold]月夜[/gold]；
///       每当你累积打出3张[purple]歌声[/purple]牌时，就随机获得1张[blue]支援[/blue]牌。
/// 代码实测：MoonPerSupport=1；SongThreshold=3；SupportPerTrigger=1
/// 升级：新增关键词 Innate
/// </summary>
public sealed class MoonBlessing : HinaModsCard
{

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    /// <summary>先古牌不参与战斗内随机生成（沿用基础游戏先古牌的约定）。</summary>
    public override bool CanBeGeneratedInCombat => false;

    /// <summary>
    /// 卡图：沿用「月之赐福」的原素材。
    /// 基类默认按「类名小写」去找 Cards/Big/ending.png（不存在，会回落到 card.png），
    /// 这里显式指向 hina_mods_tsukimi_blessing.png；文件缺失时同样安全回落到默认卡面。
    /// </summary>
    private const string PortraitFileName = "hina_mods_tsukimi_blessing.png";

    public override string CustomPortraitPath
    {
        get
        {
            string path = PortraitFileName.BigCardImagePath();
            return ResourceLoader.Exists(path) ? path : "card.png".BigCardImagePath();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MoonPerSupport", 1m),
        new DynamicVar("SongThreshold", 3m),
        new DynamicVar("SupportPerTrigger", 1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public MoonBlessing()
        : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await PowerCmd.Apply<EndingPower>(
            choiceContext,
            player.Creature,
            DynamicVars["MoonPerSupport"].BaseValue,
            player.Creature,
            this);
    }

    // 升级：获得【固有】
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
