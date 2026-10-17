using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 合唱（Chorus）
/// 本地化键：KAGUYA-CHORUS.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：单个敌人 · 关键词：歌声
/// 卡面：造成{CalculatedDamage:diff()}点伤害，每有一张[gold]合唱[/gold]伤害增加{ExtraDamage:diff()}点。
///       选择[gold]弃牌堆[/gold]中的1张牌，将其变为本牌的复制。
/// 卡图：无专属图（走默认卡图）
/// 代码实测：CalculationBase=6；ExtraDamage=3
/// 升级：基础伤害 +2（6→8）；每张合唱加成 +1（3→4）
/// </summary>
public sealed class Chorus : HinaModsCard
{
    /// <summary>歌声关键词（打出后进消耗堆，下回合抽牌前洗回抽牌堆）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(3m),
        new ExtraDamageVar(3m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(ChorusCount)
    ];

    /// <summary>场上（含各牌堆）「合唱」的张数。</summary>
    private static decimal ChorusCount(CardModel card, Creature _)
        => card.Owner.PlayerCombatState.AllCards.Count(c => c is Chorus);

    public Chorus()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 选择弃牌堆中的 1 张牌，将其变为本牌的复制
        CardModel chosen = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Discard.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1))).FirstOrDefault();

        if (chosen == null)
            return;

        // 必须用「战斗作用域」建牌：RunState.CreateCard 建出来的牌不会登记进 CombatState，
        // 而引擎规定「战斗牌堆里的牌必须已存在于 CombatState」——
        // 于是之后抽牌堆见底、弃牌堆洗回时，CardPileCmd.Add 会抛
        // 「KAGUYA-CHORUS must be added to a CombatState before adding it to this pile.」，
        // 直接打死回合循环（日志：turn loop died）→ 整场战斗卡死。
        // CardModel.CardScope 在战斗中即 CombatState，与工程内其它卡（星河入梦等）一致。
        CardModel copy = CardScope.CreateCard<Chorus>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(copy);
        }

        await CardCmd.Transform(chosen, copy);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(1m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}
