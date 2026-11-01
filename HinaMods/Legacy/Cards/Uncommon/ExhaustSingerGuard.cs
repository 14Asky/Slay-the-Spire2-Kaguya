using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
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

namespace Kaguya.HinaMods.Legacy.Cards;

// 1费 歌者专属 全体减益 + 消耗手牌获得月夜
public sealed class LegacyExhaustSingerGuard : HinaModsCard
{
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyFortunePower>(),
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<VulnerablePower>()
        };
    }
    // 严格参考你的写法：DynamicVar 数组 + PowerVar 标准定义
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<WeakPower>(2m),          // 全体虚弱 2层（不升级）
        new PowerVar<VulnerablePower>(2m),    // 全体易伤 2层（不升级）
        new PowerVar<LegacyFortunePower>(2m),       // 基础月夜：2层
        new DynamicVar("ExhaustCount", 1m)    // 基础消耗手牌：1张
    };

    // 构造函数（和参考代码格式完全统一）
    public LegacyExhaustSingerGuard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施法动画（完全照搬你的参考代码）
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 获取数值（和你的参考代码写法完全一致）
        decimal weakAmount = DynamicVars[nameof(WeakPower)].BaseValue;
        decimal vulnAmount = DynamicVars[nameof(VulnerablePower)].BaseValue;
        decimal moonAmount = DynamicVars[nameof(LegacyFortunePower)].BaseValue;
        int exhaustCount = (int)DynamicVars["ExhaustCount"].BaseValue;

        // 对全体敌人施加虚弱+易伤
        foreach (var enemy in CombatState.HittableEnemies)
        {
            // 🔥 修复：补全 choiceContext 参数
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                enemy,
                weakAmount,
                Owner.Creature,
                this);

            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                enemy,
                vulnAmount,
                Owner.Creature,
                this);
        }

        // 选择手牌消耗（官方标准写法）
        var selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, exhaustCount),
            null,
            this)).ToList();

        // 消耗选中的牌
        foreach (var card in selectedCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        // 消耗卡牌后获得月夜（严格参考你的力量施加代码）
        if (selectedCards.Count == exhaustCount)
        {
            await PowerCmd.Apply<LegacyFortunePower>(
                choiceContext,
                Owner.Creature,
                moonAmount,
                Owner.Creature,
                this);
        }
    }

    // 升级效果
    protected override void OnUpgrade()
    {
        // 消耗手牌数：1 +1 → 2张
        DynamicVars["ExhaustCount"].UpgradeValueBy(1m);
        // 月夜层数：2 +2 → 4层
        DynamicVars[nameof(LegacyFortunePower)].UpgradeValueBy(2m);
    }
}