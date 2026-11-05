using BaseLib.Utils;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 合拍（StringChorus）
/// 本地化键：KAGUYA-STRING_CHORUS.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：一名友方 · 多人联机专用
/// 卡面：选择一名[gold]友方[/gold]，使其生成一张[purple]合唱[/purple]。
///       打出后本牌变化为[purple]合唱[/purple]。
/// 升级：生成的与变化出的都改为升级后的合唱
/// </summary>
public sealed class StringChorus : HinaModsCard
{
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<Chorus>()
        };
    }

    /// <summary>多人联机专用：单人模式下不会出现在卡池里。</summary>
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public StringChorus()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 参考官方 BelieveInYou：AnyAlly 的目标一定是玩家，ThrowIfNull 后直接用 .Player。
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Player ally = cardPlay.Target.Player;

        // ① 给该友方生成一张合唱（升级则生成升级版）
        if (CombatState != null)
        {
            CardModel gift = CombatState.CreateCard<Chorus>(ally);
            ForceUpgradeIfNeeded(gift);

            // creator 传施法者本人（Owner），不是接收者 ally。
            // 官方同理：Shiv.CreateInHand(player, n, combatState, base.Owner) ——
            // 卡进谁的堆由 card.Owner 决定，creator 只用于历史记录与 AfterCardGeneratedForCombat 钩子。
            await CardPileCmd.AddGeneratedCardToCombat(
                gift, PileType.Draw, Owner, CardPilePosition.Random);
            // 每生成一张间隔一下，否则卡面会堆在同一帧、停在界面上不归位。
            await Cmd.Wait(0.1f);
        }

        // ② 本牌打出后变化为合唱。
        //    引擎的 CardCmd.Transform 要求原卡留在牌堆内，而打出中的卡位于 Play 堆，
        //    原地变形会让新卡遗留在 Play 堆无法入手；所以改成「生成到手中」的等价写法，
        //    原卡正常进入弃牌堆。
        if (cardPlay.PlayCount > 1 && cardPlay.PlayIndex < cardPlay.PlayCount - 1)
            return;   // 重放时只在最后一拍变化，避免一张牌变出多张

        CardModel created = CardScope.CreateCard<Chorus>(Owner);
        ForceUpgradeIfNeeded(created);

        await CardPileCmd.AddGeneratedCardToCombat(
            created, PileType.Hand, Owner, CardPilePosition.Top);
        await Cmd.Wait(0.1f);
    }

    /// <summary>
    /// 本牌已升级时，把生成出来的「合唱」也强制升到 1 级。
    /// 不走 CardCmd.Upgrade：它带 UI 预览副作用、且受 CombatManager.IsEnding 影响；
    /// 直接用 UpgradeInternal + FinalizeUpgradeInternal 与工程内其它卡保持一致。
    /// </summary>
    private void ForceUpgradeIfNeeded(CardModel card)
    {
        if (!IsUpgraded)
            return;
        if (card == null || !card.IsMutable || !card.IsUpgradable || card.IsUpgraded)
            return;

        card.UpgradeInternal();
        card.FinalizeUpgradeInternal();
    }
}
