#nullable enable

using Sandbox;
using System;

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

        /// <summary>
        /// The base amount of torque to apply when moving the melon.
        /// </summary>
        //[Property(Title = "Base Force")]
        public static readonly float BaseForce = 130000;

        /// <summary>
        /// The amount of torque to use while correcting for horizontal velocity.
        /// Lower values may cause the melon to orbit its target, and higher
        /// values will make it beeline directly at it.
        /// </summary>
        //[Property(Title = "Correction Force")]
        public static readonly float CorrectionForce = 500;

        public override void Spawn() {
			base.Spawn();
			SetModel("models/sbox_props/watermelon/watermelon.vmdl");
			PhysicsEnabled = true;
			UsePhysicsCollision	= true;
			CurrentMelons++;
		}

		private Vector3 RandomVelocity()
		{
			Random random = new Random();
			float randX = random.Float(min: -1, max: 1);
			float randY = random.Float(min: -1, max: 1);
			float randZ = random.Float(min: .1f, max: 1);

			return new Vector3(randX, randY, randZ).Normal;
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

        /// <summary>
        /// Move the melon towards a specific position, usually the attack target.
        /// </summary>
        /// <param name="targetPos">The target position.</param>
        public void MoveTowards(Vector3 targetPos)
        {
            targetPos = targetPos.WithZ(this.Position.z);

            Vector3 normal = (targetPos - this.Position).Normal;
            Vector2 normal2D = new Vector2(normal.x, normal.y);

            var axis = normal.RotateAround(new Vector3(0, 0, 0), Rotation.FromYaw(90));

            float torque = BaseForce;

            PhysicsBody.ApplyTorque(axis * torque);

            // determine angle to account for existing velocity
            Vector2 velocity2D = new Vector2(PhysicsBody.Velocity.x, PhysicsBody.Velocity.y);
            float angle = MeasureAngle(velocity2D, normal2D);

            float correctionMagnitude = velocity2D.Length * MathF.Sin(angle);
            PhysicsBody.ApplyTorque(normal * correctionMagnitude * CorrectionForce);

        }

        [GameEvent.Tick.Server]
		private void Tick() {
			if (ShouldUpdateTarget()) UpdateTarget();
			if (Target.IsValid()) {
				//PhysicsBody.ApplyForceAt(Position + new Vector3 {z = 10}, (Target.Position - Position).Normal * 10_000);
				MoveTowards(Target.Position);
			}
		}

        /// <summary>
        /// Return the angle created by two vectors, given they start at the same point.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float MeasureAngle(Vector2 a, Vector2 b)
        {
            float angleA = MathF.Atan2(a.y, a.x);
            float angleB = MathF.Atan2(b.y, b.x);
            return angleA - angleB;
        }

    }


}