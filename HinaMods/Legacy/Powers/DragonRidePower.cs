//using Godot;
//using MegaCrit.Sts2.Core.Combat;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Creatures;
//using MegaCrit.Sts2.Core.Entities.Players;
//using MegaCrit.Sts2.Core.Entities.Powers;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models;
//using MegaCrit.Sts2.Core.Models.Powers;
//using MegaCrit.Sts2.Core.ValueProps;
//using BaseLib.Abstracts; // 引用BaseLib基类
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Legacy.Powers;

//// 继承BaseLib的CustomPowerModel（支持自定义图标+自动注册）
//public sealed class LegacyDragonRidePower : CustomPowerModel
//{
//    // 官方原版配置
//    public override PowerType Type => PowerType.Buff;
//    public override PowerStackType StackType => PowerStackType.Counter;
//    public override bool IsInstanced => true;
//    public override bool AllowNegative => false; // 补充标准配置

//    // 🔥 自定义图标路径（小写+下划线，规范命名）
//    public override string CustomPackedIconPath => "res://images/hinamods/Powers/dragon_ride_power.png";
//    public override string CustomBigIconPath => "res://images/hinamods/Powers/dragon_ride_power.png";

//    // 官方标准伤害变量
//    protected override DynamicVar[] CanonicalVars => new[]
//    {
//        new DamageVar(21m, ValueProp.Move)
//    };

//    // 官方原版SetDamage方法
//    public void SetDamage(decimal damage)
//    {
//        AssertMutable();
//        DynamicVars.Damage.BaseValue = damage;
//    }

//    // 回合开始逻辑（无修改）
//    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
//    {
//        await base.AfterPlayerTurnStart(choiceContext, player);

//        // 仅自己的回合触发 
//        if (player != Owner.Player)
//            return;

//        // 对全体敌人造成伤害
//        await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, DynamicVars.Damage, Owner);

//        // 触发后移除
//        await PowerCmd.Remove(this);
//    }
//}