using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
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
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.SupportCards.Common;

[Pool(typeof(TokenCardPool))]
// 1费全体攻击 | 支援专属·连斩 TOKEN代币牌
public sealed class LegacySupportDoubleStrike : HinaModsCard
{
    // 基础伤害3点（无修改）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move)
    };

    // 支援专属标签（无修改）
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORT };

    // 构造函数（无修改）
    public LegacySupportDoubleStrike() : base(1, CardType.Attack, CardRarity.Token, TargetType.AllEnemies)
    {
    }

    // 核心效果：3次全体攻击（无修改）+ 修复BUFF施加
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(3)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)
            .Execute(choiceContext);
        // 🔥 修复1：补全官方强制5参数 + decimal数值规范
        await PowerCmd.Apply<LegacySupportAttackPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    // 升级逻辑（无修改）
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
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
            supportCards.Add(combatState.CreateCard<LegacySupportDoubleStrike>(owner));
        }

        // 🔥 修复3：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}