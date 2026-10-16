using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Powers;
using Kaguya.HinaMods.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 锦鳞（GoldenScale）
/// 本地化键：KAGUYA-SUPPORT_ENERGY_CHARGE.title / .description
/// 2费 · 能力牌 · 罕见 · 目标：自身
/// 卡面：回合开始时，将你抽取到的随机一张1费及以上牌费用-1。
/// 升级：费用 -1（2 → 1）
/// 说明：效果由 <see cref="GoldenScalePower"/> 承载 —— 在「抽牌阶段之后」触发（防止减费失败 / 联机不同步）；
///       打出多张可叠加（2 张 → 每回合随机 2 张，3 张 → 3 张）。
/// </summary>
public sealed class GoldenScale : HinaModsCard
{
    public GoldenScale()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<GoldenScalePower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await PowerCmd.Apply<GoldenScalePower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        // 2费 → 1费
        EnergyCost.UpgradeBy(-1);
    }
}
