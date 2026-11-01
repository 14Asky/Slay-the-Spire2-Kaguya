using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 期盼（Expectation）
/// 本地化键：KAGUYA-EXPECTATION.title / .description
/// 1费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：从[gold]抽牌堆[/gold]中选择1张牌[gold]升级[/gold]，获得3层[gold]月夜[/gold]。
/// 代码实测：UpgradeCount=1；MoonlightPower=3
/// 升级：月夜 +2
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class Expectation : HinaModsCard
{
    // 支援 + 支援牌标签（支援标签使基类自动附加虚无/消耗关键词）
    public override HashSet<string> CustomTags =>
        new HashSet<string>() { CustomCardTags.SUPPORT, CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("UpgradeCount", 1m),
        new PowerVar<MoonlightPower>(3m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public Expectation()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int count = (int)DynamicVars["UpgradeCount"].BaseValue;

        List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Draw.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, count),
            card => card.IsUpgradable)).ToList();

        if (selected.Count > 0)
        {
            CardCmd.Upgrade(selected, CardPreviewStyle.HorizontalLayout);
        }

        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(MoonlightPower)].BaseValue,
            Owner.Creature,
            this);
    }

    // 升级：月夜 3 → 5
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(MoonlightPower)].UpgradeValueBy(2m);
    }
}
