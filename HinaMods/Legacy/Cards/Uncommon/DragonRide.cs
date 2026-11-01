//using BaseLib.Utils;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using Kaguya.HinaMods;
//using Kaguya.HinaMods.Character;
//using Kaguya.HinaMods.Legacy.Powers;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Legacy.Cards;

//public sealed class LegacyDragonRide : HinaModsCard
//{
//    // 官方标准常量
//    private const string DragonDamageKey = "DragonDamage";

//    // 官方标准动态变量（无任何反编译代码）
//    protected override DynamicVar[] CanonicalVars => new[]
//    {
//        new DynamicVar(DragonDamageKey, 28m)
//    };

//    // 构造函数（2费 技能 稀有 自身目标）
//    public LegacyDragonRide()
//        : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

//    // 官方原版OnPlay逻辑
//    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//    {
//        var power = await PowerCmd.Apply<LegacyDragonRidePower>(Owner.Creature, 1m, Owner.Creature, this);
//        power.SetDamage(DynamicVars[DragonDamageKey].BaseValue);
//    }

//    // 官方原版升级逻辑
//    protected override void OnUpgrade()
//    {
//        EnergyCost.UpgradeBy(-1);
//    }
//}