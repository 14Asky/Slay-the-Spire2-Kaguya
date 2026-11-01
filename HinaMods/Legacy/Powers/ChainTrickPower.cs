using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Powers;

public sealed class LegacyChainTrickPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 层数代表每次抽牌的伤害值

    // 可选：自定义图标路径
    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/ChainTrickPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/ChainTrickPower.png";

    // 每次抽牌后触发（包括回合开始抽牌和卡牌效果抽牌）
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // 只对能力拥有者生效
        if (card.Owner.Creature != Owner) return;

        var enemies = base.CombatState.HittableEnemies;
        if (enemies.Count == 0) return;

        int damage = (int)Amount; // 层数即为伤害值
        await CreatureCmd.Damage(choiceContext, enemies, damage, ValueProp.Unpowered, Owner, null, null);

        // 播放特效和音效
        VfxCmd.PlayOnCreatureCenters(enemies, "vfx/vfx_attack_slash");
        SfxCmd.Play("slash_attack.mp3");
    }

    // 回合结束时移除自身（效果只在本回合生效）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}
