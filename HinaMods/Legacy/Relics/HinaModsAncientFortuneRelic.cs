using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
using Kaguya.HinaMods.Legacy.Cards;
using Kaguya.HinaMods.Character;
using Kaguya.HinaMods.Legacy.Relics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Relics;

// 先古版专属：继承原版所有能力 + 额外强化效果
public sealed class LegacyHinaModsAncientFortuneRelic : HinaRelics
{
    private int _savedTsukimiStacks;
    private bool _hasEightThousandTime;
    private bool _hasGivenCardsThisCombat;

    // 存档属性自动继承原遗物层数
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

    // 先古遗物品质
    public override bool ShowCounter => true;
    public override int DisplayAmount => SavedTsukimiStacks;
    public override RelicRarity Rarity => RelicRarity.Ancient;

    // 图标
    public override string PackedIconPath => "res://images/hinamods/relics/hina_mods_ancient_fortune_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/hina_mods_ancient_fortune_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/hina_mods_ancient_fortune_relic.png";

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

    private void UpdateDisplay()
    {
        InvokeDisplayAmountChanged();
    }

    // ====================== ✅ 修改：删除初始500层buff逻辑 ======================
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        if (Owner?.Creature == null) return;

        // 每场战斗开始，重置「已发牌」标记
        _hasGivenCardsThisCombat = false;

        Owner.Creature.PowerApplied += OnPowerApplied;
        Owner.Creature.PowerIncreased += OnPowerIncreased;
        Owner.Creature.PowerDecreased += OnPowerDecreased;

        // 仅应用已保存的层数，无任何默认层数
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

        if (HasEightThousandTime)
        {
            await ApplyTsukimiBlessing();
        }

        Flash();
        UpdateDisplay();
    }

    // ====================== 原版效果：层数监听（完全保留） ======================
    private void OnPowerApplied(PowerModel power) => SaveAndCheck(power);
    private void OnPowerIncreased(PowerModel power, int change, bool silent) => SaveAndCheck(power);
    private void OnPowerDecreased(PowerModel power, bool silent) => SaveTsukimiStacks(power);

    // 🔥 修复1：添加 async 关键字，用 await 调用同步方法
    private async void SaveAndCheck(PowerModel power)
    {
        SaveTsukimiStacks(power);
        await CheckEightThousandTime();
    }

    private void SaveTsukimiStacks(PowerModel power)
    {
        if (power is LegacyTsukimiTimePower tsukimi)
        {
            SavedTsukimiStacks = (int)tsukimi.Amount;
        }
    }

    // 🔥 修复2：async void → async Task（联机同步核心修复）
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
            // 每回合获得1层月夜
            await PowerCmd.Apply<LegacyFortunePower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                DynamicVars["LegacyFortunePower"].BaseValue,
                Owner.Creature,
                null
            );

            // 仅本场战斗未发牌时，生成支援牌
            if (!_hasGivenCardsThisCombat)
            {
                await CreateSupportCards(combatState);
                Flash();
            }
        }
    }

    // ====================== 生成支援牌（完全保留） ======================
    private async Task CreateSupportCards(ICombatState combatState)
    {
        if (Owner == null || combatState == null) return;

        // 创建1张【支援打击】代币牌
        CardModel strikeCard = combatState.CreateCard<LegacySupportStrike>(Owner);
        // 创建1张【疾驰】代币牌
        CardModel drawCard = combatState.CreateCard<LegacySupportDraw>(Owner);
        // 创建1张【招架】代币牌
        CardModel blockCard = combatState.CreateCard<LegacySupportHeavyBlock>(Owner);

        // 将三张卡牌添加到手牌
        // 🔥 仅补全缺失的 creator 参数（Owner），其余完全不变
        await CardPileCmd.AddGeneratedCardsToCombat([strikeCard, drawCard, blockCard], PileType.Hand, Owner);

        // 发牌后标记为「已发放」，本场战斗不再触发
        _hasGivenCardsThisCombat = true;
    }

    // 🔥 补充：战斗结束解绑事件（防内存泄漏+同步规范，原版缺失，建议加上）
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