using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

/// <summary>
/// 歌者共鸣
/// 每层效果：每回合开始时，将1张【活力迸发】加入你的手牌
/// 可叠加，永久生效
/// </summary>
public sealed class LegacySingerConvertPower : CustomPowerModel
{
    // ====================== 完全对齐 LegacySupportChannelingPower 配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/singer_convert_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/singer_convert_power.png";
    public override int DisplayAmount => (int)Amount;

    // ====================== 核心：每回合开始生成活力迸发 ======================
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.BeforeSideTurnStart(choiceContext, side, participants, combatState);

        // ✅ 修复：Owner本身就是Creature，直接使用
        if (!participants.Contains(Owner))
            return;

        // 叠加几层，就生成几张
        for (int i = 0; i < Amount; i++)
        {
            await CreateVigorBurst();
        }
    }

    // ====================== 生成卡牌方法（完全对齐官方写法） ======================
    private async Task CreateVigorBurst()
    {
        // ✅ 修复：Creature直接有Player属性，无需.Creature
        CardModel newCard = Owner.CombatState.CreateCard(ModelDb.Card<LegacyVigorBurst>(), Owner.Player);
        // 官方标准：添加到手牌
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner.Player);
    }
}