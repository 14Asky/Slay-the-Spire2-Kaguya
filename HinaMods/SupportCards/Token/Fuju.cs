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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// Fuju~（Fuju）
/// 本地化键：KAGUYA-SUPPORT_GOLD.title / .description
/// 0费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：[blue]支援[/blue]：获得15金币。
/// 升级：无
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class Fuju : HinaModsCard
{
    // 支援标签（无修改）
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    // 0费、技能、代币卡、自身目标（无修改）
    public Fuju() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 核心效果：获得10金币 + 施加1层支援之力BUFF
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int goldAmount = IsUpgraded ? 15 : 10;
        // 官方标准指令：获得10金币（无修改）
        await PlayerCmd.GainGold(goldAmount, Owner);
    }

    // 升级效果（可选，保持原样）
    protected override void OnUpgrade()
    {
    }

    // 官方标准代币生成方法
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
            supportCards.Add(combatState.CreateCard<Fuju>(owner));
        }

        // 🔥 修复3：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}