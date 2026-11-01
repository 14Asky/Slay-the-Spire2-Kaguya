using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 归流
/// 1费 技能牌 | 自身目标
/// 获得5点格挡。将弃牌堆洗入抽牌堆，下回合抽1张牌。
/// 升级：获得7点格挡。下回合抽2张牌。
/// </summary>
public sealed class LegacySingerRecycle : HinaModsCard
{
    // 歌者专属自定义标签
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };

    // 格挡标签

    // 动态变量：基础6点格挡
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move)
    ];

    // 构造函数：1费、技能、普通、自身目标
    public LegacySingerRecycle()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 播放施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 获得格挡
        await CommonActions.CardBlock(this, cardPlay);

        // 弃牌堆洗入抽牌堆
        await CardPileCmd.Shuffle(choiceContext, player);

        // 🔥 完全对标你的参考写法：三元运算符判断升级
        decimal drawBuffStacks = IsUpgraded ? 2m : 1m;
        await PowerCmd.Apply<DrawCardsNextTurnPower>(
            choiceContext,
            player.Creature,
            drawBuffStacks,
            player.Creature,
            this
        );
    }

    // 升级：格挡+2 → 8点 | 移除消耗
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
