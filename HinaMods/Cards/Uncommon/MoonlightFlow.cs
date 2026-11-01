using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 月夜流转（MoonlightFlow）
/// 本地化键：KAGUYA-HINA_MODS_MOON_DRAW.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：自身
/// 卡面：获得2层[gold]月夜[/gold]。
///       抽2张牌。
/// 代码实测：Cards=2；MoonlightPower=2
/// 升级：月夜 +1；Cards +1
/// </summary>
public sealed class MoonlightFlow : HinaModsCard
{
    

    // 基础数值：2层月夜 + 抽2张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<MoonlightPower>(2m),
        new CardsVar(2)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<MoonlightPower>(),
        };
    }

    // 无消耗限制，移除所有 playable 限制
    protected override bool IsPlayable => base.IsPlayable;
    protected override bool ShouldGlowGoldInternal => false;

    public MoonlightFlow()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 仅首次触发，防止复制/额外打出重复获得
        if (cardPlay.IsFirstInSeries)
        {
            // 严格参照参考模板：直接 Apply 获得月夜
            decimal moonAmount = base.DynamicVars["MoonlightPower"].BaseValue;
            // 🔥 唯一修复：补全官方强制参数 choiceContext
            await PowerCmd.Apply<MoonlightPower>(
                choiceContext,
                Owner.Creature,
                moonAmount,
                Owner.Creature,
                this);
        }

        // 抽牌逻辑（固定生效）
        int drawAmount = (int)base.DynamicVars.Cards.BaseValue;
        await CardPileCmd.Draw(choiceContext, drawAmount, Owner);
    }

    // 升级：1→2月夜 / 2→3抽牌
    protected override void OnUpgrade()
    {
        base.DynamicVars["MoonlightPower"].UpgradeValueBy(1m);
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}