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
// 1费技能牌 | 支援专属·敏捷  Token代币牌
/// <summary>
/// 翻跃（SupportLeap）
/// 本地化键：KAGUYA-SUPPORT_DEXTERITY.title / .description
/// 1费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：[blue]支援[/blue]：获得2层[gold]敏捷[/gold]。
/// 代码实测：DexterityPower=2
/// 升级：敏捷 +1
/// </summary>
public sealed class SupportLeap : HinaModsCard
{
    // 动态变量：敏捷（基础2层）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<DexterityPower>(2m)
    };
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
          HoverTipFactory.FromPower<DexterityPower>()
        };
    }

    // 支援专属标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    // 构造函数：1费 | 技能牌 | 代币稀有度 | 目标自身
    public SupportLeap()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.IsFirstInSeries)
        {
            // 🔥 修复1：补全官方强制5参数（上下文）
            await PowerCmd.Apply<DexterityPower>(
                choiceContext,
                base.Owner.Creature,
                base.DynamicVars["DexterityPower"].BaseValue,
                base.Owner.Creature,
                this
            );
        }
    }

    // 升级：2层 → 3层敏捷
    protected override void OnUpgrade()
    {
        base.DynamicVars["DexterityPower"].UpgradeValueBy(1m);
    }

    // ====================== 官方代币生成方法（完全照搬模板） ======================
    // 🔥 修复2：CombatState → 官方标准接口 ICombatState
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
            supportCards.Add(combatState.CreateCard<SupportLeap>(owner));
        }

        // 🔥 修复3：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}