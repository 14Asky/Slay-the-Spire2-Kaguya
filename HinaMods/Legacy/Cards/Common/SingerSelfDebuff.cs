using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 歌者自弱
/// 1费 技能牌 | 单体目标
/// 对目标施加2层易伤。
/// 升级：费用变为0。
/// </summary>
public sealed class LegacySingerSelfDebuff : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<VulnerablePower>()
        };
    }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VulnerablePower>(2m)
    ];

    public LegacySingerSelfDebuff()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        decimal enemyVuln = DynamicVars[nameof(VulnerablePower)].BaseValue;

        // 修复：添加 choiceContext 参数
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            enemyVuln,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}