using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Relics;

/// <summary>
/// 星星眼镜（StarGlassesRelic）
/// 本地化键：KAGUYA-STAR_GLASSES_RELIC.title / .description
/// 卡面：每场战斗中，你打出的第一张[purple]歌声[/purple]牌获得[gold]重放[/gold]1。
/// 代码实测：本场战斗第一次打出歌声牌时，该次打出次数 +1（每场战斗仅一次）。
/// 实现：覆写 ModifyCardPlayCount —— 该钩子在「计算打出次数」阶段执行（早于实际结算），
///       所以 +1 对【当前这张牌】当场生效。
/// </summary>
public sealed class StarGlassesRelic : HinaRelics
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override string PackedIconPath => "res://images/hinamods/relics/star_glasses_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/star_glasses_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/star_glasses_relic.png";

    /// <summary>本场战斗是否已触发过。</summary>
    private bool _usedThisCombat;

    /// <summary>
    /// 战斗内进度必须走 [SavedProperty]：不然联机重连时字段回到默认 false，
    /// 本场会再触发一次「+1 次播放」→ 两端打出次数不同 → checksum 分叉。
    /// </summary>
    [SavedProperty]
    private bool UsedThisCombat
    {
        get => _usedThisCombat;
        set
        {
            AssertMutable();
            _usedThisCombat = value;
        }
    }

    public override Task BeforeCombatStart()
    {
        UsedThisCombat = false;
        return base.BeforeCombatStart();
    }

    public override int ModifyCardPlayCount(CardModel card, Creature target, int playCount)
    {
        int result = base.ModifyCardPlayCount(card, target, playCount);

        if (UsedThisCombat) return result;
        if (card?.Owner?.Creature != Owner?.Creature) return result;

        if (card is not HinaModsCard hinaCard
            || !hinaCard.CustomTags.Contains(CustomCardTags.ECHO))
        {
            return result;
        }

        UsedThisCombat = true;
        Flash();
        return result + 1;
    }
}
