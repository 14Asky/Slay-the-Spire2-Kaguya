using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 支援锻造
/// 3费 能力牌 | 自身目标
/// 获得1层支援锻造。虚无。
/// 升级：不虚无。
/// </summary>
public sealed class LegacySupportForgeCard : HinaModsCard
{
    public LegacySupportForgeCard()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override List<CardKeyword> CanonicalKeywords => [
         CardKeyword.Ethereal
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacySupportForgePower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        // 修复：添加 choiceContext 参数
        await PowerCmd.Apply<LegacySupportForgePower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}