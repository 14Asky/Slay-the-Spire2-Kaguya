using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 开播（StartBroadcast）
/// 本地化键：KAGUYA-SINGER_GUARD.title / .description
/// 1费 · 技能牌 · 普通 · 目标：自身 · 关键词：歌声
/// 卡面：获得8点格挡。
///       作为本回合第一张打出时，获得2层[gold]月夜[/gold]。
/// 代码实测：Block=8；MoonlightPower=2
/// 升级：月夜 +1；Block +2
/// </summary>
public sealed class StartBroadcast : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<MoonlightPower>(2m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public StartBroadcast()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    /// <summary>本回合自己还没有打出过任何牌 → 这张就是本回合第一张。</summary>
    private bool IsFirstCardThisTurn
    {
        get
        {
            int playedCount = CombatManager.Instance.History.CardPlaysFinished
                .Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner);

            return playedCount == 0;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block.BaseValue,
            ValueProp.Move,
            cardPlay
        );

        if (IsFirstCardThisTurn)
        {
            await PowerCmd.Apply<MoonlightPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["MoonlightPower"].BaseValue,
                Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["MoonlightPower"].UpgradeValueBy(1m);
    }
}
