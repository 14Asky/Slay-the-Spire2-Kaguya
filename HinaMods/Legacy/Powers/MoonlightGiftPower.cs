using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

/// <summary>
/// 月夜形态：当获得或消耗【月夜】时，额外获得1层月夜
/// </summary>
public sealed class LegacyMoonlightGiftPower : CustomPowerModel
{
    // 基础配置（官方标准写法）
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    // 图标
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/moonlight_gift.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/moonlight_gift.png";

    // 递归防护锁（防止无限刷月夜）
    private bool _isProcessing;

    // ==============================================
    // 官方原生存在的钩子：【获得】月夜时触发
    // ==============================================
    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        await base.AfterApplied(applier, cardSource);

        // 仅监听：自己获得了 月夜(LegacyFortunePower)
        if (applier == Owner && !_isProcessing && cardSource == null)
        {
            try
            {
                _isProcessing = true;
                // 🔥 严格按你的参考格式编写 + 官方无上下文标准写法
                await PowerCmd.Apply<LegacyFortunePower>(new ThrowingPlayerChoiceContext(), Owner, 1m, Owner, null);
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }

    // ==============================================
    // 官方原生存在的钩子：【消耗/移除】月夜时触发
    // ==============================================
    // 🔥 严格匹配官方方法签名（无choiceContext）
    public override async Task AfterRemoved(Creature oldOwner)
    {
        await base.AfterRemoved(oldOwner);

        // 仅监听：自己失去了 月夜(LegacyFortunePower)
        if (oldOwner == Owner && !_isProcessing)
        {
            try
            {
                _isProcessing = true;
                // 🔥 严格按你的参考格式编写 + 官方无上下文标准写法
                await PowerCmd.Apply<LegacyFortunePower>(new ThrowingPlayerChoiceContext(), Owner, 1m, Owner, null);
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}