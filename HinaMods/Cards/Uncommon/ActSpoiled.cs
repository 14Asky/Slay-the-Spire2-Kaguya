using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 撒娇（ActSpoiled）
/// 本地化键：KAGUYA-EXHAUST_SINGER_GUARD.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：全体敌人 · 关键词：歌声
/// 卡面：[gold]消耗[/gold]1张牌；给予所有敌人1层[gold]虚弱[/gold]和1层[gold]易伤[/gold]。
/// 代码实测：ExhaustCount=1；WeakPower=1；VulnerablePower=1
/// 升级：虚弱 +1；易伤 +1；ExhaustCount +1
/// </summary>
public sealed class ActSpoiled : HinaModsCard
{
    /// <summary>歌声关键词（打出后进消耗堆，下回合抽牌前洗回抽牌堆）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<VulnerablePower>()
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<WeakPower>(1m),
        new PowerVar<VulnerablePower>(1m),
        new DynamicVar("ExhaustCount", 1m)
    };

    public ActSpoiled()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        decimal weakAmount = DynamicVars[nameof(WeakPower)].BaseValue;
        decimal vulnAmount = DynamicVars[nameof(VulnerablePower)].BaseValue;
        int exhaustCount = (int)DynamicVars["ExhaustCount"].BaseValue;

        // 先消耗手牌
        var selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, exhaustCount),
            null,
            this)).ToList();

        foreach (var card in selectedCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        // 再对全体敌人施加虚弱 + 易伤
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, weakAmount, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, vulnAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(WeakPower)].UpgradeValueBy(1m);          // 1 → 2
        DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1m);    // 1 → 2
        DynamicVars["ExhaustCount"].UpgradeValueBy(1m);             // 1 → 2
    }
}
