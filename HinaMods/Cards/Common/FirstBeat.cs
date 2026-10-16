using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 第一拍（FirstBeat）
/// 本地化键：KAGUYA-SINGER_STRIKE.title / .description
/// 0费 · 攻击牌 · 普通 · 目标：单个敌人 · 关键词：歌声
/// 卡面：作为本回合第一张打出时：造成11点伤害。
///       否则：造成6点伤害。
/// 代码实测：Damage=6；FirstDamage=11
/// 升级：FirstDamage +2；Damage +2
/// </summary>
public sealed class FirstBeat : HinaModsCard
{
    private bool _playedAsFirst;

    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    /// <summary>作为本回合第一张打出时不走回音（牌也不进消耗堆）。</summary>
    protected override bool ShouldArmEcho(CardPlay cardPlay) => !_playedAsFirst;

    /// <summary>
    /// 作为本回合第一张打出时【不消耗】此牌 —— 返回引擎默认的弃牌堆。
    /// 其余情况沿用基类【歌声】逻辑：进消耗堆，下回合抽牌前洗回抽牌堆。
    /// </summary>
    /// <remarks>
    /// ⚠ 时序：本方法在 <see cref="OnPlay"/> 之前执行（引擎 OnPlayWrapper 内先算结果堆、
    /// 后记历史、再执行 OnPlay），此时 <see cref="_playedAsFirst"/> 尚未赋值，
    /// 所以这里必须重新用 <see cref="IsFirstCardThisTurn"/> 判定 —— 该属性数的是
    /// CardPlaysFinished，当前这张牌在两个时点都还没进去，计数一致成立。
    /// </remarks>
    protected override CardLocation GetResultLocationForCardPlay()
    {
        if (IsFirstCardThisTurn)
            return new CardLocation(Owner, PileType.Discard, CardPilePosition.Bottom);

        return base.GetResultLocationForCardPlay();
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("FirstDamage", 11m)
    ];

    public FirstBeat()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    /// <summary>本回合自己还没有打出过任何牌 → 这张就是本回合第一张。</summary>
    private bool IsFirstCardThisTurn =>
        CombatManager.Instance.History.CardPlaysFinished
            .Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner) == 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _playedAsFirst = IsFirstCardThisTurn;

        decimal damage = _playedAsFirst
            ? DynamicVars["FirstDamage"].BaseValue
            : DynamicVars.Damage.BaseValue;

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["FirstDamage"].UpgradeValueBy(2m);
    }
}
