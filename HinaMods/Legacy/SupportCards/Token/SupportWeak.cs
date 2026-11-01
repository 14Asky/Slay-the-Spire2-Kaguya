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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

[Pool(typeof(TokenCardPool))]
// 0费技能牌 | 支援专属·虚弱  TOKEN代币牌
public sealed class LegacySupportWeak : HinaModsCard
{
    // 动态变量：仅虚弱（基础1层）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<WeakPower>(1m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<WeakPower>()
        };
    }


    //支援专属标签
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORT };

    // 构造函数：0费 | 技能牌 | 代币稀有度 | 目标单体敌人
    public LegacySupportWeak()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    { }

    // 打出逻辑：施加虚弱（对标参考代码规范）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加虚弱（仅第一次打出生效，和你的模板一致）
        if (cardPlay.IsFirstInSeries)
        {
            // 🔥 核心修复1：严格匹配官方PowerCmd.Apply 5参数API
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                cardPlay.Target,
                base.DynamicVars["WeakPower"].BaseValue,
                base.Owner.Creature,
                this
            );
        }
        // 🔥 同步修复：支援技能力量施加
        await PowerCmd.Apply<LegacySupportSkillPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    // 升级逻辑：虚弱 1层 → 2层（完全对标参考代码）
    protected override void OnUpgrade()
    {
        base.DynamicVars["WeakPower"].UpgradeValueBy(1m);
    }

    // ====================== 官方代币生成方法（完全照搬模板） ======================
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
            supportCards.Add(combatState.CreateCard<LegacySupportWeak>(owner));
        }

        // 🔥 核心修复2：删除不存在的 addedByPlayer 参数，匹配官方API
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}