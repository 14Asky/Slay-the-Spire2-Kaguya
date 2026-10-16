using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using Kaguya.HinaMods.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 玩闹（Playful）
/// 本地化键：KAGUYA-HINA_MODS_MOON_STRIKE.title / .description
/// 1费 · 攻击牌 · 基础 · 目标：单个敌人
/// 卡面：造成8点伤害。
///       获得2层[gold]月夜[/gold]。
/// 代码实测：Damage=8；MoonlightPower=2
/// 升级：月夜 +1；Damage +2
/// </summary>
public sealed class Playful : HinaModsCard, ITranscendenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<MoonlightPower>(2m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public Playful()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars["MoonlightPower"].BaseValue,
            base.Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars["MoonlightPower"].UpgradeValueBy(1m);
    }

    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<KaguyaHime>();
}
