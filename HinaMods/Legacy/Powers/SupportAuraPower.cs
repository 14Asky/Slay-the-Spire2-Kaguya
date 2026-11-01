using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacySupportAuraPower : CustomPowerModel
{
    // ====================== 基础配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 仅添加这1行：实现多层叠加+独立数据（官方唯一必需代码）
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_aura.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_aura.png";
    public override int DisplayAmount => (int)Amount;

    // ====================== 官方钩子 ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 仅玩家自身生效
        if (Owner == null || !Owner.IsPlayer || !Owner.IsAlive)
            return;

        if (cardPlay.Card.Owner != Owner.Player)
            return;

        CardModel playedCard = cardPlay.Card;
        // 筛选支援标签
        if (playedCard is not HinaModsCard modCard
            || modCard.CustomTags?.Contains(LegacyCustomCardTags.SUPPORT) != true)
        {
            return;
        }

        // 🔥 核心修复：严格匹配官方5参数API，使用自带的choiceContext
        if (playedCard.Type == CardType.Attack)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, 1m, Owner, null);
        }
        // 🔥 核心修复：严格匹配官方5参数API
        else if (playedCard.Type == CardType.Skill)
        {
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, 1m, Owner, null);
        }
    }
}