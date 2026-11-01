extends SceneTree
# Write the validated export inventory, including Spine's original source assets.

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var entries = JSON.parse_string(FileAccess.get_file_as_string(args[0]))
	var packer := PCKPacker.new()
	if packer.pck_start(args[1]) != OK:
		quit(1)
		return
	for entry in entries:
		if packer.add_file(entry.path, entry.source) != OK:
			push_error("Pack entry failed: " + entry.path)
			quit(1)
			return
	var result := packer.flush()
	print("KAGUYA_PACK_WRITTEN entries=" + str(entries.size()) + " error=" + str(result))
	quit(0 if result == OK else 1)
