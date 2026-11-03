using System;
using Godot;
using HarmonyLib;
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Visuals;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Kaguya.HinaMods.Patches;

/// <summary>
/// 经典版辉夜的序列帧动画状态机（来自 master 分支的 <c>HinaModsAnimationPatch</c>）。
///
/// 作用：引擎调用 <c>NCreature.SetAnimationTrigger("Attack"/"Hit"/...)</c> 时，
/// 把 AnimatedSprite2D 切到对应动作，并挂一个 <c>animation_finished</c> 回调切回待机。
/// **没有这个补丁，攻击/受击会停在最后一帧不动**（就是现在报的问题）。
///
/// 经典版及 Spine 失败后的序列帧回退使用此补丁。
/// 正常的新版 Spine 立绘由原生动画状态机管理。
/// </summary>
[HarmonyPatch(typeof(NCreature), "SetAnimationTrigger")]
public static class LegacyHinaAnimationPatch
{
    private const string FinalAnimPrefix = "Moons";

    public static void Postfix(NCreature __instance, string trigger)
    {
        if (!Kaguya.KaguyaModConfig.UseLegacyHina && !KaguyaSpineVisual.IsLegacyFallback(__instance.Visuals))
            return;

        if (__instance.Entity == null || !__instance.Entity.IsPlayer)
            return;

        if (__instance.Entity.ModelId.ToString() != "CHARACTER.KAGUYA-HINA_CHARACTER")
            return;

        bool isFinalForm = __instance.Entity.HasPower<LegacyTsukimiBlessingPower>();
        PlayAnimation(__instance, trigger, isFinalForm);
    }

    private static void PlayAnimation(NCreature node, string trigger, bool isFinalForm)
    {
        switch (trigger)
        {
            case "Hit":
                PlayAnim(node, "Hit", false, isFinalForm);
                break;
            case "Attack":
                PlayAnim(node, "Attack", false, isFinalForm);
                break;
            case "Cast":
                // 曾经这里传 fromEnd: true（从最后一帧倒放），打出能力牌时会看到「抽搐」——
                // 倒着播攻击动画就是这个观感。能力牌没有专属施法动作，正放攻击动作即可。
                PlayAnim(node, "Attack", false, isFinalForm);
                break;
            case "Dead":
                PlayAnim(node, "Dead", false, isFinalForm);
                break;
            default:
                PlayAnim(node, "Idle", false, isFinalForm);
                break;
        }
    }

    /// <summary>立即按当前形态刷新到待机（获得/失去月之赐福时调用）。</summary>
    public static void RefreshForm(Creature creature)
    {
        if (!Kaguya.KaguyaModConfig.UseLegacyHina)
            return;

        if (!creature.IsPlayer || creature.ModelId.ToString() != "CHARACTER.KAGUYA-HINA_CHARACTER")
            return;

        var room = MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance;
        if (room == null)
            return;

        NCreature? creatureNode = room.GetCreatureNode(creature);
        if (creatureNode == null)
            return;

        bool isFinalForm = creature.HasPower<LegacyTsukimiBlessingPower>();
        PlayAnim(creatureNode, "Idle", false, isFinalForm);
    }

    private const string HookedMeta = "kaguya_legacy_anim_hooked";

    private static void PlayAnim(NCreature node, string animName, bool fromEnd, bool isFinalForm)
    {
        Node2D? visual = node.GetNodeOrNull<Node2D>("HinaMods");
        if (visual == null)
            return;

        var anim = visual.GetNodeOrNull<AnimatedSprite2D>("Visuals");
        if (anim == null)
            return;

        string targetAnim = isFinalForm ? $"{FinalAnimPrefix}{animName}" : animName;

        // 已经在播同一个待机动画时不要重播：Play 会把帧重置到 0，
        // 而引擎可能在一次出牌里连续多次 SetAnimationTrigger，反复重置看起来就是抽搐。
        if (animName == "Idle" && anim.Animation.ToString() == targetAnim && anim.IsPlaying())
            return;

        // 动作播完回待机 —— 这个回调只连一次。
        // 旧写法每次 Play 都用 Callable.From 新建一个委托去 IsConnected / Connect，
        // 而 Callable.From 每次都产生新对象，IsConnected 永远返回 false ⇒ 旧的断不掉，
        // 每播一次就多挂一个回调。动画结束时多个回调同时 Play 待机，就是一抽一抽的来源。
        if (!anim.HasMeta(HookedMeta))
        {
            anim.SetMeta(HookedMeta, true);
            anim.Connect(
                AnimatedSprite2D.SignalName.AnimationFinished,
                Callable.From(() =>
                {
                    if (!GodotObject.IsInstanceValid(anim) || !GodotObject.IsInstanceValid(node))
                        return;
                    if (anim.Animation.ToString().EndsWith("Dead", StringComparison.Ordinal))
                        return;
                    // 按「当前是否已变身」决定待机动画，而不是按刚播完的动画名。
                    // 变身可能发生在动作播放中途（打出能力牌），那时动作名还是普通 Attack，
                    // 但结束瞬间人物已经是最终形态，必须回 MoonsIdle。
                    bool finalForm = node.Entity != null && node.Entity.HasPower<LegacyTsukimiBlessingPower>();
                    anim.Play(finalForm ? FinalAnimPrefix + "Idle" : "Idle");
                }));
        }

        anim.Frame = 0;
        anim.Play(targetAnim, 1f, fromEnd);
    }
}
