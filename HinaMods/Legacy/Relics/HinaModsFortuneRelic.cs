using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods.Character;
using Kaguya.HinaMods.Legacy.Relics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Relics;

public sealed class LegacyHinaModsFortuneRelic : HinaRelics
{
	private int _savedTsukimiStacks;
	private bool _hasEightThousandTime;

	[SavedProperty]
	public int SavedTsukimiStacks
	{
		get => _savedTsukimiStacks;
		private set
		{
			AssertMutable();
			if (_savedTsukimiStacks != value)
			{
				_savedTsukimiStacks = value;
				UpdateDisplay();
			}
		}
	}

	[SavedProperty]
	public bool HasEightThousandTime
	{
		get => _hasEightThousandTime;
		private set
		{
			AssertMutable();
			_hasEightThousandTime = value;
		}
	}

	public override bool ShowCounter => true;
	public override int DisplayAmount => SavedTsukimiStacks;
	public override RelicRarity Rarity => RelicRarity.Starter;

	public override string PackedIconPath => "res://images/hinamods/relics/hina_mods_fortune_relic.png";
	protected override string PackedIconOutlinePath => "res://images/hinamods/relics/hina_mods_fortune_relic.png";
	protected override string BigIconPath => "res://images/hinamods/relics/hina_mods_fortune_relic.png";

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new PowerVar<LegacyFortunePower>(1m),
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<LegacyFortunePower>(),
		HoverTipFactory.FromPower<LegacyTsukimiTimePower>(),
		HoverTipFactory.FromPower<LegacyTsukimiBlessingPower>(),
	};

	public override RelicModel GetUpgradeReplacement()
		=> ModelDb.Relic<LegacyHinaModsAncientFortuneRelic>();

	private void UpdateDisplay()
	{
		InvokeDisplayAmountChanged();
	}

	public override async Task BeforeCombatStart()
	{
		await base.BeforeCombatStart();
		if (Owner?.Creature == null) return;

		Owner.Creature.PowerApplied += OnPowerApplied;
		Owner.Creature.PowerIncreased += OnPowerIncreased;
		Owner.Creature.PowerDecreased += OnPowerDecreased;

		if (SavedTsukimiStacks > 0)
		{
			await PowerCmd.Apply<LegacyTsukimiTimePower>(
				new ThrowingPlayerChoiceContext(),
				Owner.Creature,
				SavedTsukimiStacks,
				Owner.Creature,
				null
			);
		}
		else
		{
			await PowerCmd.Apply<LegacyTsukimiTimePower>(
				new ThrowingPlayerChoiceContext(),
				Owner.Creature,
				80,
				Owner.Creature,
				null
			);
			SavedTsukimiStacks = 80;
		}

		if (HasEightThousandTime)
		{
			await ApplyTsukimiBlessing();
		}

		Flash();
		UpdateDisplay();
	}

	// 🔥 修复：事件同步调用 async Task 方法
	private void OnPowerApplied(PowerModel power) => SaveAndCheck(power);
	private void OnPowerIncreased(PowerModel power, int change, bool silent) => SaveAndCheck(power);
	private void OnPowerDecreased(PowerModel power, bool silent) => SaveTsukimiStacks(power);

	private async void SaveAndCheck(PowerModel power)
	{
		SaveTsukimiStacks(power);
		await CheckEightThousandTime(); // ✅ 用 await 调用同步方法
	}

	private void SaveTsukimiStacks(PowerModel power)
	{
		if (power is LegacyTsukimiTimePower tsukimi)
		{
			SavedTsukimiStacks = (int)tsukimi.Amount;
		}
	}

	// ✅ 核心修复：async void → async Task（联机同步关键）
	private async Task CheckEightThousandTime()
	{
		if (HasEightThousandTime || SavedTsukimiStacks < 8000)
			return;

		HasEightThousandTime = true;

		await ApplyTsukimiBlessing();
		Flash();
	}

	private async Task ApplyTsukimiBlessing()
	{
		if (Owner?.Creature == null) return;
		await PowerCmd.Apply<LegacyTsukimiBlessingPower>(
			new ThrowingPlayerChoiceContext(),
			Owner.Creature,
			1,
			Owner.Creature,
			null
		);
	}

	// ====================== 核心修复：更新为官方标准AfterSideTurnStart签名 ======================
	// 🔥 唯一修改：补充缺失的IReadOnlyList<Creature> participants参数
	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		// ✅ 正确调用基类方法（传入所有3个参数）
		await base.AfterSideTurnStart(side, participants, combatState);

		// 原有逻辑完全不变
		if (side == Owner?.Creature.Side)
		{
			await PowerCmd.Apply<LegacyFortunePower>(
				new ThrowingPlayerChoiceContext(),
				Owner.Creature,
				DynamicVars["LegacyFortunePower"].BaseValue,
				Owner.Creature,
				null
			);
			Flash();
		}
	}

	public override async Task AfterCombatEnd(CombatRoom _)
	{
		if (Owner?.Creature != null)
		{
			Owner.Creature.PowerApplied -= OnPowerApplied;
			Owner.Creature.PowerIncreased -= OnPowerIncreased;
			Owner.Creature.PowerDecreased -= OnPowerDecreased;
		}

		Status = RelicStatus.Normal;
		UpdateDisplay();
		await Task.CompletedTask;
	}
}
