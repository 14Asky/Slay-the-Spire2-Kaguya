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
using MegaCrit.Sts2.Core.HoverTips;
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
// 0费技能牌 | 支援专属·虚弱  TOKEN代币牌
/// <summary>
/// 怯战（Flinch）
/// 本地化键：KAGUYA-SUPPORT_WEAK.title / .description
/// 1费 · 技能牌 · 令牌 · 目标：单个敌人
/// 卡面：[blue]支援[/blue]：给予2层[gold]虚弱[/gold]。
/// 代码实测：WeakPower=2
/// 升级：费用 -1
/// </summary>
public sealed class Flinch : HinaModsCard
{
    // 动态变量：仅虚弱（基础1层）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<WeakPower>(2m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<WeakPower>()
        };
    }


    //支援专属标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    // 构造函数：0费 | 技能牌 | 代币稀有度 | 目标单体敌人
    public Flinch()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
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
    }

    // 升级：费用 -1（1 → 0）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
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
            supportCards.Add(combatState.CreateCard<Flinch>(owner));
        }

        // 🔥 核心修复2：删除不存在的 addedByPlayer 参数，匹配官方API
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}