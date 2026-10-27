using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Rare;

/// <summary>
/// 月落（MoonSet）
/// 本地化键：KAGUYA-MOON_SET.title / .description
/// 2费 · 攻击牌 · 稀有 · 目标：全体敌人 · 关键词：消耗（升级后移除）· 多人联机专用
/// 卡面：获得{MoonlightPower:diff()}层[gold]月夜[/gold]（本回合[gold]友方[/gold]每打出1张牌，额外+1层）；
///       然后对所有敌人造成等同于你[gold]月夜[/gold]层数的伤害。
/// 代码实测：MoonlightPower=1（基础），本回合友方每打出 1 张牌额外 +1
/// 升级：去除[gold]消耗[/gold]
/// </summary>
public sealed class MoonSet : HinaModsCard
{
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<MoonlightPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    /// <summary>多人联机专用：单人模式下不会出现在卡池里。</summary>
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public MoonSet()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ① 获得月夜：基础 1 层 + 本回合友方每打出 1 张牌再加 1 层
        int gain = 1 + CountAllyCardsPlayedThisTurn();

        MoonlightPower? moon = Owner.Creature.GetPower<MoonlightPower>();
        if (moon == null)
        {
            await PowerCmd.Apply<MoonlightPower>(
                choiceContext, Owner.Creature, gain, Owner.Creature, this);
        }
        else
        {
            await PowerCmd.ModifyAmount(choiceContext, moon, gain, Owner.Creature, this);
        }

        // ② 伤害 = 此刻的月夜层数（含刚刚获得的这批）
        decimal amount = Owner.Creature.GetPower<MoonlightPower>()?.Amount ?? 0m;
        if (amount <= 0m)
            return;

        await DamageCmd.Attack(amount)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }

    /// <summary>本回合「友方」已经打完的牌数（单人 = 自己）。</summary>
    private int CountAllyCardsPlayedThisTurn()
    {
        var combat = CombatState;
        if (combat == null)
            return 0;

        return CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
            entry.HappenedThisTurn(combat)
            && entry.CardPlay.Card?.Owner is Player owner
            && combat.Players.Contains(owner));
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
