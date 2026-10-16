using BaseLib.Utils;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
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
/// 桃子（Peach）
/// 本地化键：KAGUYA-HINA_MODS_LEAP.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：自身 · 标签：支援牌
/// 卡面：获得6点[gold]格挡[/gold]。下个回合开始时，随机获得1张0费[blue]支援[/blue]牌。
/// 代码实测：Block=6；PeachPower=1
/// 升级：Block +2（6 → 8）；PeachPower +1（1 → 2）
/// </summary>
public sealed class Peach : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
        new PowerVar<PeachPower>(1m)
    };

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<PeachPower>()
        };
    }

    public Peach() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        await CommonActions.CardBlock(this, cardPlay);

        // 能力负责「下个回合开始时给 0 费支援牌」，这里只负责挂上
        await PowerCmd.Apply<PeachPower>(
            choiceContext,
            player.Creature,
            DynamicVars[nameof(PeachPower)].BaseValue,
            player.Creature,
            this);
    }

    /// <summary>升级：格挡 6 → 8，支援牌张数 1 → 2。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars[nameof(PeachPower)].UpgradeValueBy(1m);
    }
}
