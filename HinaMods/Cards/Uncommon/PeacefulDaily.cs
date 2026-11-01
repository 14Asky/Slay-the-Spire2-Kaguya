
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 平静日常（PeacefulDaily）
/// 本地化键：KAGUYA-DAILY_SONG.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：自身 · 关键词：歌声
/// 卡面：获得8点[gold]格挡[/gold]。
///       回合结束时，若你本回合未打出过攻击牌，则将随机一张[purple]歌声[/purple]牌加入[gold]抽牌堆[/gold]。
/// 代码实测：Block=8
/// 升级：Block +2
/// </summary>
public sealed class PeacefulDaily : HinaModsCard
{
    /// <summary>歌声关键词（打出后进消耗堆，下回合抽牌前洗回抽牌堆）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    public PeacefulDaily()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null || player.Creature == null)
            return;

        // 获得格挡
        await CommonActions.CardBlock(this, cardPlay);

        // 回合结束结算：若本回合未打出过攻击牌，则加一张歌声牌进抽牌堆
        await PowerCmd.Apply<DailySongEndPower>(
            choiceContext,
            player.Creature,
            1m,
            player.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
