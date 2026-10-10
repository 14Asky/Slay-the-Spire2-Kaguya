using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// 统一命名空间
namespace Kaguya.HinaMods.Cards;

public sealed class NoteCombo : HinaModsCard
{
    // 严格参考Ricochet 动态变量格式
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move)
    };

    // 歌者专属标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SINGER };

    // 构造函数 完全对齐Ricochet
    public NoteCombo()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    // 核心打出逻辑 1:1复刻Ricochet + 你的消耗堆写法
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 完全照搬你的代码 获取玩家
        Player player = Owner;
        if (player == null)
            return;

        // 🔥 严格使用你提供的 消耗堆获取方式
        CardPile exhaustPile = PileType.Exhaust.GetPile(player);
        List<CardModel> singerCards = exhaustPile.Cards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(CustomCardTags.SINGER) == true)
            .Cast<CardModel>()
            .ToList();

        // 总攻击次数：基础1次 + 消耗堆歌者牌数量
        int totalHits = 1 + singerCards.Count;

        // 官方Ricochet 原版链式伤害调用
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(totalHits)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(base.CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // 升级逻辑 对齐Ricochet
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}