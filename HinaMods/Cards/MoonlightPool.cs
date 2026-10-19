using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 「可以获得月夜的牌」的检索池，供「辉夜姬」「月扉」共用。
///
/// 两个口径（别混用）：
///   · <see cref="GrantersFromCardPool"/> —— **卡池**：所有能获得月夜的牌（「辉夜姬」用它直接生成新卡）。
///   · <see cref="FromCombatPiles"/>     —— **牌堆**：当前战斗里抽/弃/耗三堆的实际卡牌（「月扉」用它移动）。
///   ⚠ 不要用 <c>player.Deck</c>：它是 <c>CardPile(PileType.Deck)</c>，开战时内容已被搬进抽牌堆，战斗中是空的。
///
/// 判定方式（两者取并集）：
///   1. 已知「会发放月夜」的卡 → <see cref="KnownGainers"/>（代码里直接
///      <c>PowerCmd.Apply&lt;MoonlightPower&gt;</c> 而动态变量没有对应键的卡，如 游戏人间，必须靠白名单）；
///   2. 动态变量里带 <c>MoonlightPower</c> / <c>MoonlightGain</c> / <c>DrawnMoonlight</c> 的卡（新卡自动命中）。
///   ⚠「消耗月夜」的变量叫 <c>MoonCost</c>，不能算作获取。
/// </summary>
public static class MoonlightPool
{
    /// <summary>动态变量里代表「获得月夜」的键名（MoonCost 是消耗，排除）。</summary>
    private static readonly HashSet<string> GainVarKeys = new(StringComparer.Ordinal)
    {
        "MoonlightPower",   // PowerVar<MoonlightPower>(n)
        "MoonlightGain",    // DynamicVar("MoonlightGain", n)（如 归途）
        "DrawnMoonlight",   // 天意：抽到时获得
    };

    /// <summary>已知会发放月夜的卡（与「代码里 Apply&lt;MoonlightPower&gt;」的集合一一对应）。</summary>
    private static readonly HashSet<Type> KnownGainers = new()
    {
        typeof(Expectation),            // 期盼
        typeof(HeavensWill),            // 天意
        typeof(MoonlightFlow),          // 月夜流转
        typeof(Omurice),                // 蛋包饭
        typeof(Playful),                // 玩闹
        typeof(PlayfulLife),            // 游戏人间
        typeof(ReturnFlow),             // 归流
        typeof(SameMoonDifferentTime),  // 同月殊时
        typeof(StartBroadcast),         // 开播
        typeof(TurnBack),               // 回眸
        typeof(Unbelievable),           // 难以置信
        typeof(WayHome),                // 归途
        typeof(YachiyoClone),           // 八千代分身
    };

    /// <summary>这张牌是否会给你月夜。</summary>
    public static bool CanGrantMoonlight(CardModel? card)
    {
        if (card == null)
            return false;

        if (KnownGainers.Contains(card.GetType()))
            return true;

        // 兜底：标准写法的新卡靠动态变量识别；若某模型在非战斗上下文取 DynamicVars 会抛异常，就当它不算
        try
        {
            return card.DynamicVars.Keys.Any(k => GainVarKeys.Contains(k));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>卡池里所有能获得月夜的牌（模板，用于生成新卡）。</summary>
    public static List<CardModel> GrantersFromCardPool()
    {
        return ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c => c.CanBeGeneratedInCombat)
            .Where(c => CanGrantMoonlight(c))
            .Cast<CardModel>()
            .ToList();
    }

    /// <summary>当前战斗里、抽牌堆 / 弃牌堆 / 消耗堆中能获得月夜的牌（战斗内的实际卡牌，用于移动）。</summary>
    public static List<CardModel> FromCombatPiles(Player? player)
    {
        List<CardModel> result = [];
        if (player == null)
            return result;

        PileType[] piles = [PileType.Draw, PileType.Discard, PileType.Exhaust];
        foreach (PileType pileType in piles)
        {
            CardPile? pile = pileType.GetPile(player);
            if (pile == null)
                continue;
            result.AddRange(pile.Cards.Where(CanGrantMoonlight));
        }

        return result;
    }
}
