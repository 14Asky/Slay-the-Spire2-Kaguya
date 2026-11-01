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
/// 支援光环
/// 2费 能力牌 | 自身目标
/// 获得1层支援光环。
/// 升级：固有。
/// </summary>
public sealed class LegacySupportAuraCard : HinaModsCard
{
    // 构造：2费 能力牌 稀有 目标自身
    public LegacySupportAuraCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacySupportAuraPower>(),
        };
    }

    // 使用卡牌：施加支援光环力量
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 官方施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        // 修复：添加 choiceContext 参数
        await PowerCmd.Apply<LegacySupportAuraPower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            this);
    }

    // 升级：固有
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}