using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 安可（Encore）
/// 本地化键：KAGUYA-ENCORE.title / .description
/// 1费 · 能力牌 · 稀有 · 目标：自身
/// 卡面：每回合使你打出的前1张牌返回[gold]手牌[/gold]。
/// 代码实测：EncorePower=1
/// 升级：EncorePower +1
/// </summary>
public sealed class Encore : HinaModsCard
{
    // 动态变量：每回合回收的牌数（基础1，升级2）
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<EncorePower>(1m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<EncorePower>()
        };
    }

    public Encore() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<EncorePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(EncorePower)].BaseValue,
            Owner.Creature,
            this);

        // 本能力自身是 CardType.Power，EncorePower 计数时已按「能力牌不计入」排除，
        // 因此这里无需（也不应）再记录来源卡引用。
    }

    // 升级：每回合回收的牌数 +1（1 → 2）
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(EncorePower)].UpgradeValueBy(1m);
    }
}
