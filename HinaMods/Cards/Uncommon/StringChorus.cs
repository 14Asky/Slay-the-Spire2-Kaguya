using BaseLib.Utils;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
#if HAS_SAKAYORI
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System;
using System.Linq;
using System.Reflection;
#endif

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 弦乐合唱（StringChorus）
/// 本地化键：KAGUYA-STRING_CHORUS.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：一名友方 · 多人联机专用
/// 卡面：选择一名[gold]友方[/gold]，将一张[gold]合唱[/gold]加入其[gold]抽牌堆[/gold]。
///       将本牌打出后变化为[gold]合唱[/gold]。
/// 升级：加入的与变化出的都改为升级后的合唱
/// 代码实测：OverworkReduce=2；Cards=2（仅合并工程；独立辉夜 mod 没有彩叶，见 #if）
/// </summary>
public sealed class StringChorus : HinaModsCard
{
    private const string OverworkReduceVar = "OverworkReduce";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
#if HAS_SAKAYORI
        new DynamicVar(OverworkReduceVar, 2m),
#endif
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    /// <summary>多人联机专用：单人模式下不会出现在卡池里。</summary>
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public StringChorus()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? ally = cardPlay.Target?.Player;
        if (ally == null)
            return;

        // ① 给该友方的抽牌堆加入一张合唱（升级则加升级版）
        var allyCombat = ally.Creature?.CombatState;
        if (allyCombat != null)
        {
            CardModel gift = allyCombat.CreateCard<Chorus>(ally);
            ForceUpgradeIfNeeded(gift);

            await CardPileCmd.AddGeneratedCardToCombat(
                gift, PileType.Draw, ally, CardPilePosition.Random);
        }

#if HAS_SAKAYORI
        // ② 目标是「酒寄彩叶」：过劳 -2，并让该友方抽 2 张
        //    ⚠ 只有合并工程（Yachiyo）有彩叶角色与 Overwork 这个 Power；独立辉夜 mod 里整段不编译。
        if (IsSakayori(ally))
        {
            await ReduceOverworkAsync(choiceContext, ally.Creature, DynamicVars[OverworkReduceVar].BaseValue);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, ally);
        }
#endif

        // ③ 【变化】本牌打出后变化为合唱。
        //    参考 HisadaIzumi/HisadaIzumiCode/cards/HisadaIzumiCard.cs 的 TransformIntoHand：
        //    引擎的 CardCmd.Transform 要求原卡留在牌堆内，而打出中的卡位于 Play 堆，
        //    原地变形会让新卡遗留在 Play 堆无法入手；所以这里改成「生成到手中」的等价写法，
        //    原卡正常进入弃牌堆。
        if (cardPlay.PlayCount > 1 && cardPlay.PlayIndex < cardPlay.PlayCount - 1)
            return;   // 重放时只在最后一拍变化，避免一张牌变出多张

        CardModel created = CardScope.CreateCard<Chorus>(Owner);
        ForceUpgradeIfNeeded(created);

        await CardPileCmd.AddGeneratedCardToCombat(
            created, PileType.Hand, Owner, CardPilePosition.Top);
    }

    /// <summary>
    /// 本牌已升级时，把生成出来的「合唱」也强制升到 1 级。
    /// 之所以不用 <c>CardCmd.Upgrade</c>：它带 UI 预览副作用、且受 CombatManager.IsEnding 影响；
    /// 直接走 UpgradeInternal + FinalizeUpgradeInternal 与工程内其它卡（辉夜姬 / 合唱）保持一致，最稳。
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

#if HAS_SAKAYORI
    /// <summary>该玩家是不是彩叶（按角色 Id 判断，避免与彩叶侧类型强耦合）。</summary>
    private static bool IsSakayori(Player player)
    {
        string? id = player.Character?.Id.Entry;
        return !string.IsNullOrEmpty(id)
               && id.Contains("SAKAYORI_IROHA", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 让目标「过劳」减少 <paramref name="amount"/> 层。
    /// 用反射拿彩叶侧的 Overwork 类型，避免硬引用；找不到时静默跳过。
    /// </summary>
    private static async Task ReduceOverworkAsync(PlayerChoiceContext ctx, Creature? target, decimal amount)
    {
        if (target == null || amount <= 0m)
            return;

        Type? overworkType = AccessTools.TypeByName("Kaguya.Powers.Overwork");
        if (overworkType == null)
            return;

        MethodInfo? powerGetter = typeof(ModelDb).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "Power" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
        if (powerGetter == null)
            return;

        if (powerGetter.MakeGenericMethod(overworkType).Invoke(null, null) is not PowerModel overwork)
            return;

        await PowerCmd.Apply(ctx, overwork, target, -amount, target, null, false);
    }
#endif
}
