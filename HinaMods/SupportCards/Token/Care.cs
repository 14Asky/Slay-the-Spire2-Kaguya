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

/// <summary>
/// 照顾（Care）
/// 本地化键：KAGUYA-SUPPORT_BLOCK.title / .description
/// 0费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：[blue]支援[/blue]：获得4点格挡。
/// 代码实测：Block=4
/// 升级：Block +2
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class Care : HinaModsCard
{
    // 支援标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    // 🔥 修复1：格挡数值改为官方强制 decimal 类型 (4m)
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, ValueProp.Move)];

    // 核心修改：稀有度改为 Token（代币，和 Shiv/支援打击 完全一致）
    public Care() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 原有效果：获得格挡 + 1层支援之力
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }

    // 原有升级效果
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }

    // 官方标准：代币生成方法（和 SupportStrike 代码完全一致）
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
            supportCards.Add(combatState.CreateCard<Care>(owner));
        }

        // 🔥 修复4：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}