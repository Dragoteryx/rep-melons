#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace RepMelons {

	/// <summary>
	/// A utility class to access targets.
	/// </summary>
	public static class Target {

		[ConVar.Replicated("rep_melons_ignore_players")]
		public static bool IgnorePlayers { get; private set; }

		private static List<Entity?> Targets = new();

		/// <summary>
		/// List containing all available targets.
		/// </summary>
		public static IEnumerable<Entity> All {
			get {
				return from ent in Targets
					where ent.IsTarget()
					select ent;
			}
		}

		/// <summary>
		/// Update the target list. This is called automatically every tick.
		/// </summary>
		[Event.Tick.Server]
		public static void Update() {
			Targets = new();
			foreach (var ent in Entity.All) {
				if (ent.IsTarget()) Targets.Add(ent);
			}
		}

		/// <summary>
		/// Check whether this entity is a valid target.
		/// </summary>
		public static bool IsTarget([NotNullWhen(true)] this Entity? ent) {
			if (ent.IsProp() || ent.IsRagdoll()) return !RepMelon.ReachedMaxMelons();
			if (ent.IsPlayer()) return !IgnorePlayers;
			return false;
		}

		/// <summary>
		/// Check whether this entity is a prop.
		/// </summary>
		public static bool IsProp([NotNullWhen(true)] this Entity? ent) {
			return ent.IsValid() && ent.ClassName == "prop_physics";
		}

		/// <summary>
		/// Check whether this entity is a ragdoll.
		/// </summary>
		public static bool IsRagdoll([NotNullWhen(true)] this Entity? ent) {
			return ent.IsValid() && ent.ClassName == "prop_ragdoll";
		}

		/// <summary>
		/// Check whether this entity is a player.
		/// </summary>
		public static bool IsPlayer([NotNullWhen(true)] this Entity? ent) {
			return ent.IsValid() && ent.Tags.Has("player") && ent.Health > 0;
		}

	}

}