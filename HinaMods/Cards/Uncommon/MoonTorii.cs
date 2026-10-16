using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 月之鸟居（MoonTorii）
/// 本地化键：KAGUYA-HINA_MODS_MOON_BARRIER.title / .description
/// 2费 · 技能牌 · 罕见 · 目标：自身
/// 卡面：获得12点格挡。
///       [gold]消耗[/gold]1层[gold]月夜[/gold]：给予所有敌人1层[gold]虚弱[/gold]，并使其失去1层[gold]临时力量[/gold]。
/// 代码实测：Block=12；MoonCost=1；DebuffAmount=1
/// 升级：MoonCost +1；DebuffAmount +1
/// </summary>
public class MoonTorii() : HinaModsCard(2,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12m, ValueProp.Move),
        new DynamicVar("MoonCost", 1m),
        new DynamicVar("DebuffAmount", 1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<MoonlightPower>(),
           HoverTipFactory.FromPower<WeakPower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 基础格挡
        await CommonActions.CardBlock(this, cardPlay);

        int moonCost = (int)DynamicVars["MoonCost"].BaseValue;
        int debuff = (int)DynamicVars["DebuffAmount"].BaseValue;

        // 消耗月夜（被「获得+免费」途径取得时 need=0，效果照常触发）
        if (!await TryConsumeMoonlight(choiceContext, moonCost, this))
        {
            return;
        }
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        foreach (var enemy in CombatState.HittableEnemies)
        {
            // 给予虚弱
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, debuff, Owner.Creature, this);

            // 使其失去等量的临时力量
            var tempStrength = enemy.GetPower<TemporaryStrengthPower>();
            if (tempStrength != null)
            {
                await PowerCmd.ModifyAmount(choiceContext, tempStrength, -debuff, Owner.Creature, this);
            }
        }
    }

    // 升级：月夜消耗 +1，虚弱/临时力量层数 +1
    protected override void OnUpgrade()
    {
        DynamicVars["MoonCost"].UpgradeValueBy(1m);
        DynamicVars["DebuffAmount"].UpgradeValueBy(1m);
    }
}
