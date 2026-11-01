extends Node2D

## 辉夜 Spine 预览场景的驱动脚本（仅用于在编辑器里看动画，不参与游戏逻辑）。
##
## 骨架里的动画：idle_loop(10.67s) / idle_loop_backup(5.33s) / attack / cast / hurt / die
## 想换动画：改下面的 ANIMATION；或运行后在远程场景树里手动调 set_animation。
## 想整段播放一遍再停下来看：把 LOOP 改成 false。

const ANIMATION := "idle_loop"
const LOOP := true
## 0 表示从头播；改成 0.5 之类可以停在中段看姿势
const START_TIME := 0.0


func _ready() -> void:
	var sprite := get_node_or_null("SpineSprite")
	if sprite == null:
		push_warning("[Spine 预览] 找不到 SpineSprite 节点")
		return

	var state = sprite.get_animation_state()
	if state == null:
		push_warning("[Spine 预览] 骨架数据没加载上（先检查 kaguya_spine_data.tres 的 skel/atlas 是否指对）")
		return

	state.set_animation(ANIMATION, LOOP, 0)
	state.update(START_TIME)
	state.apply(sprite.get_skeleton())
