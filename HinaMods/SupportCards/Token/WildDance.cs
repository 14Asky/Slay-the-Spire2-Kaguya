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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

// 满月涤荡：1费 全体攻击 Token代币 支援牌
/// <summary>
/// 乱舞（WildDance）
/// 本地化键：KAGUYA-FULL_MOON_SWEEP_CARD.title / .description
/// 1费 · 攻击牌 · 令牌 · 目标：全体敌人
/// 卡面：[blue]支援[/blue]：对所有敌人造成8点伤害。
/// 代码实测：Damage=8
/// 升级：Damage +3
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class WildDance : HinaModsCard
{
    // 支援自定义标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    // 基础全体伤害：8点
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];

    // 🔥 修复1：修正C#非法构造函数语法
    public WildDance() : base(1, CardType.Attack, CardRarity.Token, TargetType.AllEnemies)
    { }

    // 打出：全体伤害 + 获得支援打击BUFF
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
    }

    // 升级：8点 → 11点（+3）
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    // ====================== 官方代币生成方法 ======================
    // 🔥 修复3：CombatState 改为官方标准接口 ICombatState
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
            supportCards.Add(combatState.CreateCard<WildDance>(owner));
        }

        // 🔥 修复4：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}
