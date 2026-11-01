using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
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

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyStarNightFree : HinaModsCard
{
    

    // 双变量设计：获得月夜层数 + 免费触发阈值（唯一Key，无重复崩溃）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<LegacyFortunePower>(2m),
        new CardsVar(2),
        new EnergyVar(2),
        new PowerVar<LegacyFortunePower>("FortuneThreshold", 16m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacyFortunePower>(),
           base.EnergyHoverTip,
        };
    }

    public LegacyStarNightFree()
        : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromDeckDraw)
    {
        await base.AfterCardDrawn(ctx, card, fromDeckDraw);
        if (card == this && Owner != null)
        {
            // 月夜 >= 阈值 → 本回合免费
            if (Owner.Creature.GetPowerAmount<LegacyFortunePower>() >= (int)DynamicVars["FortuneThreshold"].BaseValue)
            {
                SetToFreeThisTurn();
            }
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int moonAmount = (int)base.DynamicVars["LegacyFortunePower"].BaseValue;
        int drawCount = (int)base.DynamicVars.Cards.BaseValue;
        int energyAmount = (int)base.DynamicVars.Energy.BaseValue;

        // 🔥 唯一修复：补全官方强制参数 choiceContext
        await PowerCmd.Apply<LegacyFortunePower>(
            choiceContext,
            Owner.Creature,
            moonAmount,
            Owner.Creature,
            this);

        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        await PlayerCmd.GainEnergy(energyAmount, Owner);
    }

    // 升级：免费阈值降低（更容易触发免费）
    protected override void OnUpgrade()
    {
        DynamicVars["FortuneThreshold"].UpgradeValueBy(-4m);
    }
}