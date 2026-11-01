using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Rare;

public sealed class LegacySingerFugue : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move)
    };

    // 单体目标卡牌
    public LegacySingerFugue()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 目标合法性校验
        if (cardPlay.Target is not Creature target || target.IsDead)
            return;

        decimal damageValue = DynamicVars.Damage.BaseValue;
        Creature attacker = Owner.Creature;
        const int HIT_COUNT = 3;

        for (int i = 0; i < HIT_COUNT; i++)
        {
            // 攻击中途死亡则中断
            if (attacker.IsDead || target.IsDead)
                break;

            // ✅ 核心修复：正确异步调用 + 原生ToList()，无任何扩展方法报错
            IEnumerable<DamageResult> damageResults = await CreatureCmd.Damage(
                choiceContext: choiceContext, targets: new List<Creature> { target }, amount: damageValue, props: ValueProp.Move, cardSource: this, dealer: attacker, cardPlay: null);
            List<DamageResult> results = damageResults.ToList();

            // 命中判定：未格挡伤害>0 获得1能量
            foreach (DamageResult result in results)
            {
                if (result != null && result.UnblockedDamage > 0)
                {
                    await PlayerCmd.GainEnergy(1, Owner);
                }
            }

            // 攻击间隔（优化手感，可选）
            await Cmd.CustomScaledWait(0.1f, 0.15f);
        }
    }

    // 升级效果：伤害+2
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}