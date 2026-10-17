using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Cards.Uncommon;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 辉夜姬（KaguyaHime）
/// 本地化键：KAGUYA-HINA_MODS_MOON_FINAL_STRIKE.title / .description
/// 1费 · 攻击牌 · 先古 · 目标：单个敌人
/// 卡面：造成20点伤害。
///       将随机1张可以获得[gold]月夜[/gold]的牌加入手牌，使其获得[gold]消耗[/gold]和[gold]升级[/gold]并可以免费打出。
/// 代码实测：Damage=20；GiftCount=1
/// 升级：GiftCount +1；Damage +5
/// 随机池：见 GetMoonlightCardTemplates（仅攻击牌与技能牌，不含本卡自己）
/// </summary>
public sealed class KaguyaHime : HinaModsCard
{
    private const string GiftCountKey = "GiftCount";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(24m, ValueProp.Move),
        new DynamicVar(GiftCountKey, 1m),
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public KaguyaHime()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        List<CardModel> pool = GetMoonlightCardTemplates();
        if (pool.Count == 0) return;

        int count = (int)DynamicVars[GiftCountKey].BaseValue;

        // 确定性 RNG：联机下各客户端结果一致
        List<CardModel> gifts = CardFactory.GetDistinctForCombat(
            player, pool, count, player.RunState.Rng.CombatCardGeneration).ToList();

        foreach (CardModel card in gifts)
        {
            // 升级：拿到的是战斗中可变克隆（CreateCard），幂等处理
            if (card.IsMutable && card.IsUpgradable && !card.IsUpgraded)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }

            // 获得【消耗】
            card.AddKeyword(CardKeyword.Exhaust);

            // 本回合可以免费打出
            card.SetToFreeThisTurn();

            await CardPileCmd.AddGeneratedCardToCombat(
                card, PileType.Hand, player, CardPilePosition.Top);
        }
    }

    /// <summary>
    /// 「可以获得月夜的牌」随机池 —— 仅攻击牌与技能牌，不含本卡自己。
    /// 判定依据：代码里会给玩家 <c>PowerCmd.Apply&lt;MoonlightPower&gt;</c>（即「获得月夜」）的卡。
    /// 新增/修改此类卡时需同步维护本清单。
    /// </summary>
    private static List<CardModel> GetMoonlightCardTemplates()
    {
        return new List<CardModel>
        {
            // ---- 攻击牌（5）----
            ModelDb.Card<Playful>(),       // 玩闹
            ModelDb.Card<Unbelievable>(),        // 难以置信
            ModelDb.Card<WayHome>(),            // 归途
            ModelDb.Card<SameMoonDifferentTime>(),        // 同月殊时
            ModelDb.Card<HeavensWill>(),           // 天意
            // ---- 技能牌（8）----
            ModelDb.Card<YachiyoClone>(),     // 八千代分身
            ModelDb.Card<Expectation>(),              // 期盼
            ModelDb.Card<PlayfulLife>(),                 // 游戏人间
            ModelDb.Card<MoonlightFlow>(),         // 月夜流转
            ModelDb.Card<ReturnFlow>(),           // 归流
            ModelDb.Card<AllRoundSupport>(),         // 八方援护
            ModelDb.Card<StartBroadcast>(),              // 开播
            ModelDb.Card<TurnBack>(),            // 回眸
        };
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
        DynamicVars[GiftCountKey].UpgradeValueBy(1m);
    }
}
