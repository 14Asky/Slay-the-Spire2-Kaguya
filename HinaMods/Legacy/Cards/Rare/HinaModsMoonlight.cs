using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Rare;

/// <summary>
/// 月华
/// 0费 技能牌 | 自身目标
/// 获得1层月夜契约。消耗。
/// 升级：保留。
/// </summary>
public sealed class LegacyHinaModsMoonlight : HinaModsCard
{
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyTsukimiContractPower>()
        };
    }

    public LegacyHinaModsMoonlight()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 修复：添加 choiceContext 参数
        await PowerCmd.Apply<LegacyTsukimiContractPower>(
            choiceContext,
            base.Owner.Creature,
            1m,
            base.Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}