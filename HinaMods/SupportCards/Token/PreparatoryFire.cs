using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.SupportCards.Common;

[Pool(typeof(TokenCardPool))]
// 1费全体攻击 | 支援专属·连斩 TOKEN代币牌
/// <summary>
/// 预备射击（PreparatoryFire）
/// 本地化键：KAGUYA-SUPPORT_DOUBLE_STRIKE.title / .description
/// 2费 · 攻击牌 · 令牌 · 目标：全体敌人
/// 卡面：[blue]支援[/blue]：对所有敌人造成3点伤害3次。
/// 代码实测：Damage=3；HitCount=3
/// 升级：HitCount +1
/// </summary>
public sealed class PreparatoryFire : HinaModsCard
{
    // 基础伤害3点（无修改）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
        new DynamicVar("HitCount", 3m)
    };

    // 支援专属标签（无修改）
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    // 构造函数：2费
    public PreparatoryFire() : base(2, CardType.Attack, CardRarity.Token, TargetType.AllEnemies)
    {
    }

    // 核心效果：全体攻击 3 次（升级后 4 次）+ 支援之力
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(base.DynamicVars["HitCount"].IntValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)
            .Execute(choiceContext);
    }

    // 升级：命中次数 3 → 4（伤害不变）
    protected override void OnUpgrade()
    {
        base.DynamicVars["HitCount"].UpgradeValueBy(1m);
    }

    // 🔥 修复2：CombatState 改为官方标准接口 ICombatState
    public static async Task<CardModel> CreateInHand(Player owner, ICombatState combatState)
    {
        return (await CreateInHand(owner, 1, combatState)).FirstOrDefault();
    }

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, ICombatState combatState)
    {
        if (count == 0 || CombatManager.Instance.IsOverOrEnding)
            return System.Array.Empty<CardModel>();

        List<CardModel> supportCards = new List<CardModel>();
        for (int i = 0; i < count; i++)
        {
            supportCards.Add(combatState.CreateCard<PreparatoryFire>(owner));
        }

        // 🔥 修复3：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}