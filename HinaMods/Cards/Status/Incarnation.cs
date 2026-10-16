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

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 化身（Incarnation）
/// 本地化键：KAGUYA-RELIEF.title / .description
/// 1费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：移除你的全部debuff 1层。
/// 代码实测：RemoveAmount=1
/// 升级：RemoveAmount +1
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class Incarnation : HinaModsCard
{
    // 支援 + 支援牌标签（支援标签使基类自动附加虚无/消耗关键词）
    public override HashSet<string> CustomTags =>
        new HashSet<string>() { CustomCardTags.SUPPORT, CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("RemoveAmount", 1m)
    ];

    public Incarnation()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int amount = (int)DynamicVars["RemoveAmount"].BaseValue;
        if (amount <= 0)
            return;

        // 仅筛选【自己身上】的负面 BUFF
        List<PowerModel> debuffs = Owner.Creature.Powers
            .Where(power => power != null
                         && power.Type == PowerType.Debuff
                         && power.Owner == Owner.Creature)
            .ToList();

        foreach (PowerModel debuff in debuffs)
        {
            await PowerCmd.ModifyAmount(choiceContext, debuff, -amount, Owner.Creature, this);
        }
    }

    // 升级：移除层数 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars["RemoveAmount"].UpgradeValueBy(1m);
    }
}
