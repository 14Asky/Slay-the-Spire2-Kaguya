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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

// 代币池标签（支援牌必备）
[Pool(typeof(TokenCardPool))]
public sealed class LegacySupportHeal : HinaModsCard
{
    // 1. 支援专属标签（必备）
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORT };

    // 3. 动态变量：治疗2点，升级3点
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(2m)];

    // 4. 构造函数：0费/1费、技能、代币稀有度、自身目标（标准支援牌配置）
    public LegacySupportHeal() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 5. 核心效果：回复生命 + 施加支援之力BUFF（支援牌必备双效果）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 回复生命值（参考松饼）
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.IntValue);
        // 🔥 修复1：补全官方5参数 + decimal数值
        await PowerCmd.Apply<LegacySupportSkillPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    // 6. 升级逻辑：2→3点治疗
    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(1m);
    }

    // 7. 官方标准代币生成方法（支援牌必备，可直接生成到手牌）
    // 🔥 修复2：CombatState 改为官方接口 ICombatState
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
            supportCards.Add(combatState.CreateCard<LegacySupportHeal>(owner));
        }

        // 🔥 修复3：删除不存在的 addedByPlayer 参数，匹配官方API
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}