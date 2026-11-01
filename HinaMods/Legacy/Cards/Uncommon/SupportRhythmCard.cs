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
/// 支援·节律 - 能力牌
/// 3费 → 升级2费 | 保留 | 支援标签
/// </summary>
public sealed class LegacySupportRhythmCard : HinaModsCard
{
    // 构造：2费 能力牌 稀有 目标自身
    public LegacySupportRhythmCard() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacySupportRhythmPower>(),
        };
    }

    // 使用卡牌：施加节律BUFF
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 官方施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 🔥 唯一修复：补全官方强制参数 choiceContext
        await PowerCmd.Apply<LegacySupportRhythmPower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            this);
    }

    // 升级：费用-1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}