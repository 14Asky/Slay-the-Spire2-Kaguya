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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

// 代币池标签（支援牌强制要求）
[Pool(typeof(TokenCardPool))]
public sealed class LegacySupportIntangible : HinaModsCard
{
    // 支援专属标签（必备）
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORT };

    // 动态变量：1层无实体，升级后2层
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<IntangiblePower>(1m)
    ];

    // 标准支援牌构造：3费 技能 代币卡 自身目标
    public LegacySupportIntangible() : base(3, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 核心效果：获得无实体 + 施加支援之力BUFF（支援牌必备）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 🔥 修复1：补全官方5参数 + 规范动态变量调用
        await PowerCmd.Apply<IntangiblePower>(
            choiceContext,
            Owner.Creature,
            base.DynamicVars["IntangiblePower"].BaseValue,
            Owner.Creature,
            this
        );
        // 🔥 修复2：同步修正支援技能施加（补全上下文+decimal数值）
        await PowerCmd.Apply<LegacySupportSkillPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    // 升级：无实体层数 +1（1→2）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    // 官方标准代币生成方法（必备，可直接生成到手牌）
    // 🔥 修复3：CombatState 改为官方接口 ICombatState
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
            supportCards.Add(combatState.CreateCard<LegacySupportIntangible>(owner));
        }

        // 🔥 修复4：删除不存在的 addedByPlayer 参数，匹配官方API
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}