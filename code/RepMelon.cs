#nullable enable

using Sandbox;

namespace RepMelons {

	[Spawnable]
	[Library("npc_rep_melon", Title = "Replicator Melon", Group = "NPC")]
	public class RepMelon : ModelEntity {

		[ConVar.Replicated("rep_melons_max")]
		public static int MaxMelons { get; private set; } = 30;
		public static int CurrentMelons { get; private set; }

		public static bool ReachedMaxMelons() {
			return MaxMelons <= CurrentMelons;
		}

		[ConCmd.Admin("rep_melons_count")]
		public static void Count() {
			int count = 0;
			foreach (var ent in All) {
				if (ent is RepMelon) {
					count++;
				}
			}

			Log.Info($"Counted {count} melons.");
		}

		[ConCmd.Admin("rep_melons_remove_all")]
		public static void RemoveAll() {
			int count = 0;
			foreach (var ent in All) {
				if (ent is RepMelon) {
					ent.Delete();
					count++;
				}
			}

			Log.Info($"Removed {count} melons.");
		}

		public override void Spawn() {
			base.Spawn();
			SetModel("models/sbox_props/watermelon/watermelon.vmdl");
			PhysicsEnabled = true;
			UsePhysicsCollision	= true;
			CurrentMelons++;
		}

		protected override void OnDestroy() {
			base.OnDestroy();
			CurrentMelons--;
		}

		// Physics

		protected override void OnPhysicsCollision(CollisionEventData eventData) {
			var ent = eventData.Other.Entity;
			if (ent.IsTarget()) {
				if (ent.IsPlayer()) {
					var pos = ent.Position;
					ent.TakeDamage(DamageInfo.Generic(ent.Health).WithAttacker(this));
					if (!ReachedMaxMelons()) {
						RepMelon _ = new() {
							Position = pos
						};
					}
				} else {
					var pos = ent.Position;
					ent.Delete();
					RepMelon _ = new() {
						Position = pos
					};
				}
			}
		}

		public override void TakeDamage(DamageInfo info) {
			if (!info.HasTag("physics_impact")) {
				PhysicsBody.ApplyImpulseAt(info.Position, info.Force * 100);
			}
		}

		// Targeting

		private Entity? Target;
    private System.DateTime LastTargetUpdate;

		private bool ShouldUpdateTarget() {
			return (Target is not null && !Target.IsValid)
				|| (System.DateTime.Now - LastTargetUpdate).Seconds >= 1;
		}

		public bool UpdateTarget() {
			Entity? closest = null;
			foreach (var ent in RepMelons.Target.All) {
				if (!closest.IsValid() || Position.Distance(closest.Position) > Position.Distance(ent.Position)) {
					if (!Trace.Ray(WorldSpaceBounds.Center, ent.WorldSpaceBounds.Center).WorldOnly().Run().Hit) {
						closest = ent;
					}
				}
			}

			Target = closest;
			LastTargetUpdate = System.DateTime.Now;
			return closest.IsValid();
		}

		[Event.Tick.Server]
    private void Tick() {
			if (ShouldUpdateTarget()) UpdateTarget();
			if (Target.IsValid()) {
				PhysicsBody.ApplyForceAt(Position + new Vector3 {z = 10}, (Target.Position - Position).Normal * 10_000);
			}
    }
		
	}

}