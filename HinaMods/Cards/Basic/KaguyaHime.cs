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
/// 随机池：MoonlightPool.GrantersFromCardPool（全卡池里能获得月夜的牌，直接生成，不看稀有度）
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

        // 池子：**卡池**里「能获得月夜」的牌（不是牌堆里的实例，也不按稀有度过滤——
        // 玩闹是 Basic，若走 CardFactory.GetDistinctForCombat 的 FilterForCombat 会被吃掉）
        List<CardModel> pool = MoonlightPool.GrantersFromCardPool();
        if (pool.Count == 0)
            return;

        int count = (int)DynamicVars[GiftCountKey].BaseValue;

        // 确定性 RNG：联机下各客户端结果一致
        List<CardModel> picked = pool.ToList();
        player.RunState.Rng.CombatCardGeneration.Shuffle(picked);
        if (picked.Count > count)
            picked = picked.GetRange(0, count);

        MegaCrit.Sts2.Core.Combat.ICombatState? state = player.Creature.CombatState;
        if (state == null)
            return;

        foreach (CardModel template in picked)
        {
            // 直接**生成**一张新卡（不是把牌堆里那张挪过来）
            CardModel card = state.CreateCard(template, player);

            // 升级：拿到的是升级版
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

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
        DynamicVars[GiftCountKey].UpgradeValueBy(1m);
    }
}
