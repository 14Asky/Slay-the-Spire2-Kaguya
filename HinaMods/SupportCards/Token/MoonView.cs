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
/// 月见（MoonView）
/// 本地化键：KAGUYA-CONCERTED.title / .description
/// 0费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：[blue]支援[/blue]：获得1。
/// 代码实测：Energy=1
/// 升级：Energy +1
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class MoonView : HinaModsCard
{
    // 🔥 修复1：能量变量改为官方强制 decimal 类型 (1m)
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            base.EnergyHoverTip
        };
    }

    // 支援标签（无修改）
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORT };

    // 0费 技能牌 | TOKEN代币 | 目标自身（无修改）
    public MoonView() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 核心效果：获得能量 + 支援BUFF
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    // 升级：+1能量（无修改）
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);
    }

    // 代币生成方法
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
            supportCards.Add(combatState.CreateCard<MoonView>(owner));
        }

        // 🔥 修复4：删除官方不存在的 addedByPlayer 参数
        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}