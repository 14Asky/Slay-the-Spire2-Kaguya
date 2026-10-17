using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 音符连击（NoteCombo）
/// 本地化键：KAGUYA-NOTE_COMBO.title / .description
/// 0费 · 攻击牌 · 罕见 · 目标：随机敌人 · 关键词：歌声
/// 卡面：造成5点伤害1次。
///       本回合你每打出一张[purple]歌声[/purple]牌，此牌攻击次数+1。
/// 代码实测：Damage=5
/// 升级：Damage +1
/// </summary>
public sealed class NoteCombo : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move)
    };

    // 回音标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    public NoteCombo()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 本回合已打出的回音牌数量（本张牌尚在结算中，不计入）
        // 用 Owner.Creature.CombatState 而非 CombatManager.Instance.DebugOnlyGetState()：
        // 后者在联机/非战斗上下文可能返回 null，会让 HappenedThisTurn 恒为 false（计数永远为 0）。
        ICombatState? combatState = Owner.Creature.CombatState;
        int echoPlayedThisTurn = combatState == null ? 0 : CombatManager.Instance.History.CardPlaysFinished
            .Count(e => e.CardPlay.Card.Owner == player
                && e.HappenedThisTurn(combatState)
                && e.CardPlay.Card is HinaModsCard hc && hc.HasEcho);

        int totalHits = 1 + echoPlayedThisTurn;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(totalHits)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(base.CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
