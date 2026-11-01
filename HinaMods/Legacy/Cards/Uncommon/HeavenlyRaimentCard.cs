using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Uncommon;

public sealed class LegacyHeavenlyRaimentCard : HinaModsCard
{
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacyMoonlightBlockPower>(),
           HoverTipFactory.FromPower<LegacyFortunePower>(),
        };
    }

    // 🔥 新增：动态变量控制月夜格挡层数（基础3层，对标参考代码格式）
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<LegacyMoonlightBlockPower>(2m)
    };

    // 1费 能力牌 稀有
    public LegacyHeavenlyRaimentCard()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 🔥 替换：固定数值改为动态变量
        await PowerCmd.Apply<LegacyMoonlightBlockPower>(
            choiceContext,
            player.Creature,
            DynamicVars[nameof(LegacyMoonlightBlockPower)].BaseValue,
            player.Creature,
            this);
    }

    // 🔥 升级：动态变量+1（完全对标月见形态升级逻辑）
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(LegacyMoonlightBlockPower)].UpgradeValueBy(1m);
    }
}