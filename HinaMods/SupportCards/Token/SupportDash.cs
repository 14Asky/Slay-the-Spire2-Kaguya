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
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.SupportCards.Common;

/// <summary>
/// 疾驰（SupportDash）
/// 本地化键：KAGUYA-SUPPORT_DRAW.title / .description
/// 0费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：抽{DrawCount:diff()}张牌。
/// 代码实测：Int=_drawKey
/// 升级：DrawCount +1
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class SupportDash : HinaModsCard
{
    private const string _drawKey = "DrawCount";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar(_drawKey, 1)
    };

    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    public SupportDash() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // STS2强制施法动画（保留你的优化）
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 抽牌逻辑（保留你的优化）
        await CardPileCmd.Draw(choiceContext, DynamicVars[_drawKey].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[_drawKey].UpgradeValueBy(1);
    }

    // 🔥 修复2：CombatState → 官方接口 ICombatState
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
            supportCards.Add(combatState.CreateCard<SupportDash>(owner));
        }

        // 🔥 修复3：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}