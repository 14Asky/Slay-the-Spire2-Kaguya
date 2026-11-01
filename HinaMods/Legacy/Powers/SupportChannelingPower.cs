using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacySupportChannelingPower : CustomPowerModel
{
    // ====================== 【官方同款】可叠加配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 改为可叠加
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 图标不变
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_channeling.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_channeling.png";

    // 显示层数（和官方一致）
    public override int DisplayAmount => (int)Amount;

    // 触发标记（每回合重置）
    private bool _hasTriggeredAttack;
    private bool _hasTriggeredSkill;

    // ====================== 钩子逻辑完全不变 ======================
    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        if (Owner == null || !Owner.IsPlayer || !Owner.IsAlive)
            return;

        _hasTriggeredAttack = false;
        _hasTriggeredSkill = false;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player != Owner.Player)
            return;

        // 每回合重置触发
        _hasTriggeredAttack = false;
        _hasTriggeredSkill = false;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        _hasTriggeredAttack = false;
        _hasTriggeredSkill = false;
    }

    // ====================== 【核心】多层触发，生成对应数量支援牌 ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        if (Owner == null || !Owner.IsPlayer || !Owner.IsAlive)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;

        CardModel playedCard = cardPlay.Card;

        // 第一张攻击牌：按层数生成支援打击
        if (playedCard.Type == CardType.Attack && !_hasTriggeredAttack)
        {
            _hasTriggeredAttack = true;
            // 叠加几层，就生成几张
            for (int i = 0; i < Amount; i++)
            {
                await CreateSupportStrike();
            }
        }

        // 第一张技能牌：按层数生成支援格挡
        if (playedCard.Type == CardType.Skill && !_hasTriggeredSkill)
        {
            _hasTriggeredSkill = true;
            // 叠加几层，就生成几张
            for (int i = 0; i < Amount; i++)
            {
                await CreateSupportBlock();
            }
        }
    }

    // ====================== 生成卡牌代码【仅修复参数错误】 ======================
    private async Task CreateSupportStrike()
    {
        CardModel newCard = Owner.CombatState.CreateCard(ModelDb.Card<LegacySupportStrike>(), Owner.Player);
        // 🔥 修复：删除不存在的 addedByPlayer 参数，严格匹配官方API
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner.Player);
    }

    private async Task CreateSupportBlock()
    {
        CardModel newCard = Owner.CombatState.CreateCard(ModelDb.Card<LegacySupportBlock>(), Owner.Player);
        // 🔥 修复：删除不存在的 addedByPlayer 参数，严格匹配官方API
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner.Player);
    }
}