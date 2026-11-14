extends SceneTree
# Lossless binary-resource compression; encoded audio packets remain unchanged.

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 3 or not ProjectSettings.load_resource_pack(args[0]):
		push_error("Expected source PCK, inventory JSON and output directory")
		quit(1)
		return
	var entries: Array = JSON.parse_string(FileAccess.get_file_as_string(args[1]))
	var output: String = args[2]
	DirAccess.make_dir_recursive_absolute(output)
	var receipts: Array = []
	for entry in entries:
		var resource_path: String = entry.path
		if not resource_path.ends_with(".oggvorbisstr") and not resource_path.ends_with(".mp3str"):
			continue
		var original := load(resource_path) as AudioStream
		if original == null:
			push_error("Audio load failed: " + resource_path)
			quit(1)
			return
		var destination := output.path_join(resource_path.get_file())
		if ResourceSaver.save(original, destination, ResourceSaver.FLAG_COMPRESS) != OK:
			quit(1)
			return
		var roundtrip := ResourceLoader.load(destination, "", ResourceLoader.CACHE_MODE_IGNORE) as AudioStream
		var equal_packets := false
		if original is AudioStreamMP3 and roundtrip is AudioStreamMP3:
			equal_packets = original.data == roundtrip.data
		elif original is AudioStreamOggVorbis and roundtrip is AudioStreamOggVorbis:
			var a: Variant = original.packet_sequence.get("packet_data")
			var b: Variant = roundtrip.packet_sequence.get("packet_data")
			equal_packets = a != null and b != null and var_to_bytes(a) == var_to_bytes(b)
		if not equal_packets or original.get_length() != roundtrip.get_length():
			push_error("Audio verification failed: " + resource_path)
			quit(1)
			return
		for setting in ["loop", "loop_offset", "bpm", "beat_count", "bar_beats"]:
			if original.get(setting) != roundtrip.get(setting):
				push_error("Audio playback setting changed: " + resource_path + " / " + setting)
				quit(1)
				return
		var before := FileAccess.get_file_as_bytes(resource_path).size()
		var after := FileAccess.get_file_as_bytes(destination).size()
		receipts.append({"path":resource_path,"source":destination,"before":before,"after":after,
			"packetsIdentical":true,"seconds":original.get_length(),"useCompressed":after < before,
			"sourceSha256":FileAccess.get_sha256(resource_path),"resultSha256":FileAccess.get_sha256(destination)})
	var report := FileAccess.open(output.path_join("report.json"), FileAccess.WRITE)
	report.store_string(JSON.stringify(receipts, "  "))
	print("AUDIO_RESOURCE_COMPRESSION_OK count=" + str(receipts.size()))
	quit()
