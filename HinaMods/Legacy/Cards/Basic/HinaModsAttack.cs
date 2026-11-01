using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

// 自定义攻击卡牌：继承模组基础卡牌，定义费用1、攻击类型、基础稀有度、目标为任意敌人
public class LegacyHinaModsAttack() : HinaModsCard(1,
	CardType.Attack, CardRarity.Basic,
	TargetType.AnyEnemy)
{
	// 卡牌核心标签：标记为【打击】标签
	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	// 卡牌基础数值：造成6点攻击伤害
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];

	// 打出卡牌时执行：对目标造成标准卡牌攻击伤害
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
	}

	// 卡牌升级效果：伤害值提升2点
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
