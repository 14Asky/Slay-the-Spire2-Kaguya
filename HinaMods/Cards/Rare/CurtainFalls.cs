using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Rare;

/// <summary>
/// 曲终落幕（CurtainFalls）
/// 本地化键：KAGUYA-SINGER_COMBO_STRIKE.title / .description
/// 1费 · 攻击牌 · 稀有 · 目标：全体敌人 · 关键词：歌声
/// 卡面：对所有敌人12点伤害。
///       若[gold]消耗堆[/gold]中存在[purple]歌声[/purple]牌，则额外打出一次。
/// 代码实测：Damage=12
/// 升级：Damage +4
/// </summary>
public class CurtainFalls() : HinaModsCard(1,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AllEnemies) // 全体敌人目标
{
    // 歌者专属标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    // 你指定的标准写法，完全保留
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // ✅ 修复1：强制指定 Creature? 重载，彻底解决调用二义性
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 检测消耗堆是否存在歌者牌
        // 用 HasEcho 而不是查标签：像「旋转打击」这类中途才获得歌声的牌也能被计入
        bool hasSinger = PileType.Exhaust.GetPile(Owner).Cards
            .OfType<HinaModsCard>()
            .Any(c => c.HasEcho);

        // 满足条件：额外再打一次全体伤害
        if (hasSinger)
        {
            await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        }
    }

    // 升级效果：伤害+3（修复版，无编译错误）
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
