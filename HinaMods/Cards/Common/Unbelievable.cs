using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 难以置信（Unbelievable）
/// 本地化键：KAGUYA-HINA_MODS_CLEAR_GAME.title / .description
/// 1费 · 攻击牌 · 普通 · 目标：单个敌人
/// 卡面：造成3点伤害2次。
///       每造成一次未被格挡的伤害，获得1层[gold]月夜[/gold]。
/// 代码实测：Damage=3；HitCount=2
/// 升级：HitCount +1
/// </summary>
public sealed class Unbelievable : HinaModsCard
{
    private const string HitCountKey = "HitCount";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new DynamicVar(HitCountKey, 2m)
    ];

    public Unbelievable()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hitCount = DynamicVars[HitCountKey].IntValue;

        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx(null, null, SfxHitName)   // 每段命中都响声
            .Execute(choiceContext);

        // 每有一次「打穿了格挡」的伤害，就获得1层月夜
        int unblockedHits = attack.Results.Count(hit => hit.Any(r => r.UnblockedDamage > 0));

        if (unblockedHits > 0)
        {
            await PowerCmd.Apply<MoonlightPower>(
                choiceContext,
                Owner.Creature,
                unblockedHits,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[HitCountKey].UpgradeValueBy(1m);
    }
}
