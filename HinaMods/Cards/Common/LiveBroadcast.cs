using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 直播演出（LiveBroadcast）
/// 本地化键：KAGUYA-SINGER_RECYCLE.title / .description
/// 2费 · 技能牌 · 普通 · 目标：自身 · 关键词：歌声
/// 卡面：获得10点格挡。
///       将你[gold]弃牌堆[/gold]中所有牌洗入[gold]抽牌堆[/gold]，然后抽1张牌。
/// 代码实测：Block=10
/// 升级：Block +3
/// </summary>
public sealed class LiveBroadcast : HinaModsCard
{
    /// <summary>歌声关键词（按需求为本牌添加）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10m, ValueProp.Move)
    ];

    public LiveBroadcast()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await CommonActions.CardBlock(this, cardPlay);

        await CardPileCmd.Shuffle(choiceContext, player);

        await CardPileCmd.Draw(choiceContext, 1, player);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
