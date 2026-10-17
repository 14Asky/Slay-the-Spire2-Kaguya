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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

[Pool(typeof(TokenCardPool))]
// 重拳：2费 单体攻击 Token代币 支援牌
/// <summary>
/// 全垒打（HomeRun）
/// 本地化键：KAGUYA-HEAVY_STRIKE_CARD.title / .description
/// 2费 · 攻击牌 · 令牌 · 目标：单个敌人
/// 卡面：[blue]支援[/blue]：造成18点伤害，并获得1点[gold]力量[/gold]。
/// 代码实测：Damage=18；StrengthPower=1
/// 升级：Damage +4（18→22）；力量 +1（1→2）
/// </summary>
public sealed class HomeRun : HinaModsCard
{
    // 支援自定义标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    // 基础单体伤害 18 点 + 1 点力量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(18m, ValueProp.Move),
        new PowerVar<StrengthPower>(1m)
    ];

    // 🔥 修复1：修正C#构造函数语法错误（原版写法非法）
    public HomeRun() : base(2, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    { }

    // 打出：单体伤害 + 获得支援打击BUFF
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 额外获得力量
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(StrengthPower)].IntValue,
            Owner.Creature,
            this);
    }

    // 升级：18→22（+4）；力量 1→2（+1）
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars[nameof(StrengthPower)].UpgradeValueBy(1m);
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
            supportCards.Add(combatState.CreateCard<HomeRun>(owner));
        }

        // 🔥 修复4：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}
