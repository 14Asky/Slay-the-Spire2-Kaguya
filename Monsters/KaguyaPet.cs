using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Kaguya.Monsters
{
    public sealed class KaguyaPet : CustomMonsterModel
    {
        public override int MinInitialHp => 9999;
        public override int MaxInitialHp => 9999;
        public override bool IsInteractable => false;

        // 使用不含 Spine 的静态场景
        protected override string VisualsPath => "res://scenes/monsters/kaguya_pet.tscn";

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var idleState = new MoveState("IDLE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
            idleState.FollowUpState = idleState;
            return new MonsterMoveStateMachine(new List<MonsterState> { idleState }, idleState);
        }

        // 不再重写 GenerateAnimator、SetupSkins 等方法，让基类使用默认无 Spine 的行为
    }
}
