using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

[Pool(typeof(StatusCardPool))]
public sealed class LegacyRelief : HinaModsCard
{
    // 与迷茫、寂寞完全统一的状态牌配置
    public override int MaxUpgradeLevel => 0;
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override bool HasTurnEndInHandEffect => false;
    protected override IEnumerable<string> ExtraRunAssetPaths => Enumerable.Empty<string>();
    protected override IEnumerable<DynamicVar> CanonicalVars => Enumerable.Empty<DynamicVar>();

    // 官方标准状态牌构造
    public LegacyRelief()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.Self)
    { }

    // 抽到触发逻辑（统一格式）
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        if (card == this && Owner != null && Owner.Creature != null)
        {
            await Cmd.Wait(0.25f);

            // 🔥 核心修复：仅筛选【自己身上】的负面BUFF
            List<PowerModel> debuffs = Owner.Creature.Powers
                .Where(power => power != null
                             && power.Type == PowerType.Debuff
                             && power.Owner == Owner.Creature) // 严格绑定：BUFF属于自己
                .ToList();

            // 移除自己的负面BUFF
            foreach (PowerModel debuff in debuffs)
            {
                await PowerCmd.Remove(debuff);
            }
        }
    }

    // 无法打出，空实现
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }

    // 无升级效果
    protected override void OnUpgrade()
    {
    }
}