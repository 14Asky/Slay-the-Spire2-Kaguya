using Kaguya.HinaMods.Character;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Relics;

/// <summary>
/// 月见伞（HinaModsAncientFortuneRelic）
/// 本地化键：KAGUYA-HINA_MODS_ANCIENT_FORTUNE_RELIC.title / .description
/// 卡面：回合开始时，获得2层[gold]月夜[/gold]。
/// 代码实测：MoonlightPower=2
/// </summary>
public sealed class HinaModsAncientFortuneRelic : HinaRelics
{
	public override RelicRarity Rarity => RelicRarity.Ancient;

	public override string PackedIconPath => "res://images/hinamods/relics/hina_mods_ancient_fortune_relic.png";
	protected override string PackedIconOutlinePath => "res://images/hinamods/relics/hina_mods_ancient_fortune_relic.png";
	protected override string BigIconPath => "res://images/hinamods/relics/hina_mods_ancient_fortune_relic.png";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<MoonlightPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<MoonlightPower>()
	];

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		await base.AfterSideTurnStart(side, participants, combatState);

		if (side == Owner?.Creature.Side)
		{
			await PowerCmd.Apply<MoonlightPower>(
				new ThrowingPlayerChoiceContext(),
				Owner.Creature,
				DynamicVars["MoonlightPower"].BaseValue,
				Owner.Creature,
				null
			);
			Flash();
		}
	}
}
