using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

[Pool(typeof(StatusCardPool))]
// 歌声状态牌 | 抽到获得活力 | 无法打出
public sealed class LegacySinger : HinaModsCard
{
    // 状态牌标准配置
    public override int MaxUpgradeLevel => 0;
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];
    public override bool HasTurnEndInHandEffect => false;
    protected override IEnumerable<string> ExtraRunAssetPaths => Enumerable.Empty<string>();

    // 🔥 标准化动态变量（完全对标 LegacySupportDexterity 格式）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VigorPower>(3m) // 基础：3点活力
    ];

    // 悬浮提示（保持不变）
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<VigorPower>()
        };
    }

    // 构造函数（保持不变）
    public LegacySinger()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.Self)
    { }

    // 核心：抽到获得活力（🔥 标准化动态变量调用）
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        if (card == this && Owner != null)
        {
            await Cmd.Wait(0.25f);

            // 🔥 标准调用：nameof 强类型，杜绝字符串错误
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[nameof(VigorPower)].BaseValue,
                Owner.Creature,
                this
            );
        }
    }

    // 无法打出，空实现
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }
}