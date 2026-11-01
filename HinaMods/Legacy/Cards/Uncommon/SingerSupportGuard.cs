using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacySingerSupportGuard : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };


    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    public LegacySingerSupportGuard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡（普通5点，升级8点）
        await CommonActions.CardBlock(this, cardPlay);

        // 【永久触发】无论是否升级，都自选1张手牌歌声牌升级
        CardModel selectedCard = (
            await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1),
                card => card is HinaModsCard c && c.CustomTags.Contains(LegacyCustomCardTags.SINGER),
                this)
        ).FirstOrDefault();

        if (selectedCard != null)
        {
            CardCmd.Upgrade(selectedCard);
        }
    }

    protected override void OnUpgrade()
    {
        // 仅升级格挡数值：5→8
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
