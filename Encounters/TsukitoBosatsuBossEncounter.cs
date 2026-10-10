using Godot;
using Kaguya.Monsters;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using System.Collections.Generic;

namespace Kaguya.Encounters
{
	public sealed class TsukitoBosatsuBossEncounter : EncounterModel
	{
		public override RoomType RoomType => RoomType.Boss;

		public override string CustomBgm => string.Empty;

		public override string BossNodePath => "res://images/packed/map/tsukito_bosatsu_icon";

		public override MegaSkeletonDataResource BossNodeSpineResource => null;

		public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TsukitoBosatsu>() };

		protected override bool HasCustomBackground => true;

		public override float GetCameraScaling() => 0.85f;

		public override Vector2 GetCameraOffset() => Vector2.Down * 70f;

		protected override IReadOnlyList<(MonsterModel, string)> GenerateMonsters()
		{
			return new[] { (ModelDb.Monster<TsukitoBosatsu>().ToMutable(), (string)null) };
		}
	}
}
