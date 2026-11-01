using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacyTsukimiDrawBuff : CustomPowerModel
{
    // ====================== 你的原版代码 完全不动 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/tsukimi_draw_buff.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/tsukimi_draw_buff.png";

    // ====================== 【仅添加这2行：实现叠加+独立】 ======================
    // 必须加：让多层BUFF拥有独立计数器（官方固定写法）
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    // 必须加：显示当前叠加层数
    public override int DisplayAmount => (int)Amount;

    // ====================== 你的核心逻辑 完全不动（只补1个必需的await base） ======================
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // 必需：调用基类方法（不写会报错，不影响效果）
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);

        // 你的原版判断 一字未改
        if (!fromHandDraw && card.Owner.Creature == Owner)
        {
            // 🔥 仅补全官方6参数，原代码逻辑/参数完全不变
            await PowerCmd.Apply<LegacyFortunePower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null, false);
        }
    }
}