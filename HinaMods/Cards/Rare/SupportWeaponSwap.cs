using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 交换武器（SupportWeaponSwap）
/// 本地化键：KAGUYA-SUPPORT_AURA_CARD.title / .description
/// 2费 · 能力牌 · 稀有 · 目标：自身 · 标签：支援牌
/// 卡面：当你打出[blue]支援[/blue]牌获得的正面buff变为永久。
/// 升级：获得【固有】。
/// 说明：本牌只负责挂上开关能力 <see cref="WeaponSwapPower"/>；
///       实际转换逻辑在卡牌基类 HinaModsCard.GrantSupportBonus
///       （检测到该能力时直接给永久力量/敏捷，不再给临时力量/敏捷）。
/// </summary>
public sealed class SupportWeaponSwap : HinaModsCard
{
    /// <summary>支援牌标签（本牌与支援牌体系联动）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    public SupportWeaponSwap() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<WeaponSwapPower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 官方施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await PowerCmd.Apply<WeaponSwapPower>(
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
