using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 游戏人间（PlayfulLife）
/// 本地化键：KAGUYA-GAME_LIFE.title / .description
/// 0费 · 技能牌 · 罕见 · 目标：自身
/// 卡面：获得当前回合数的[gold]月夜[/gold]和格挡。
/// 升级：无
/// </summary>
public sealed class PlayfulLife : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    // 构造函数：0费 技能 罕见 目标自身
    public PlayfulLife()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 当前回合数（升级后 +1）
        int round = CombatState.RoundNumber + (IsUpgraded ? 1 : 0);

        // 获得等同于回合数的月夜
        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            Owner.Creature,
            round,
            Owner.Creature,
            this);

        // 获得等同于回合数的格挡
        await CreatureCmd.GainBlock(Owner.Creature, round, ValueProp.Unpowered, cardPlay, fast: true);
    }
}
