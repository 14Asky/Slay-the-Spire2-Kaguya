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
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 回眸（TurnBack）
/// 本地化键：KAGUYA-STAR_NIGHT_FREE.title / .description
/// 3费 · 技能牌 · 罕见 · 目标：自身
/// 卡面：获得2层[gold]月夜[/gold]，抽2张牌，获得2。
///       当你抽这张牌且拥有30层及以上[gold]月夜[/gold]时，这张牌可以被免费打出。
/// 代码实测：Cards=2；Energy=2；MoonlightPower=2；FortuneThreshold=30
/// 升级：FortuneThreshold -10
/// </summary>
public sealed class TurnBack : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<MoonlightPower>(2m),
        new CardsVar(2),
        new EnergyVar(2),
        new PowerVar<MoonlightPower>("FortuneThreshold", 30m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<MoonlightPower>(),
           base.EnergyHoverTip,
        };
    }

    public TurnBack()
        : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(ctx, card, fromHandDraw);
        if (card == this && Owner != null)
        {
            // 月夜 >= 阈值 → 本回合免费
            if (Owner.Creature.GetPowerAmount<MoonlightPower>() >= (int)DynamicVars["FortuneThreshold"].BaseValue)
            {
                SetToFreeThisTurn();
            }
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int moonAmount = (int)base.DynamicVars["MoonlightPower"].BaseValue;
        int drawCount = (int)base.DynamicVars.Cards.BaseValue;
        int energyAmount = (int)base.DynamicVars.Energy.BaseValue;

        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            Owner.Creature,
            moonAmount,
            Owner.Creature,
            this);

        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        await PlayerCmd.GainEnergy(energyAmount, Owner);
    }

    // 升级：免费阈值降低（30 → 20）
    protected override void OnUpgrade()
    {
        DynamicVars["FortuneThreshold"].UpgradeValueBy(-10m);
    }
}
