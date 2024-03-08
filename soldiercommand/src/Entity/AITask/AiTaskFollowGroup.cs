using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoldierCommand {
	public class AiTaskFollowGroup : AiTaskBase {
		private EntityArcher thisEnt;
		protected EntityPlayer leaderEnt;
		protected List<Entity> herdEnts;

		float moveSpeedNear = 0.03f;
		float moveSpeedFarAway = 0.03f;

		protected float range = 8f;
		protected float maxDistance = 3f;
		protected float arriveDistance = 3f;
		protected bool allowStrayFromHerdInCombat = true;
		protected bool allowHerdConsolidation = false;
		protected float consolidationRange = 40f;

		string moveNearAnimation = "Walk";
		string moveFarAnimation = "Sprint";

		// Data for entities this ai is allowed to consolidate its herd with.
		protected HashSet<string> consolidationEntitiesByCodeExact = new HashSet<string>();
		protected string[] consolidationEntitiesByCodePartial = new string[0];
		protected bool stuck = false;
		protected bool stopNow = false;
		protected bool allowTeleport = true;
		protected float teleportAfterRange;
		protected bool herdLeaderSwimmingLastFrame = false;
		protected Vec3d targetOffset = new Vec3d();
		float stepHeight = 1.2f;
		public AiTaskFollowGroup(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			//moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
			range = taskConfig["searchRange"].AsFloat(8f);
			maxDistance = taskConfig["maxDistance"].AsFloat(3f);
			arriveDistance = taskConfig["arriveDistance"].AsFloat(3f);
			allowStrayFromHerdInCombat = taskConfig["allowStrayFromHerdInCombat"].AsBool(true);
			allowHerdConsolidation = taskConfig["allowHerdConsolidation"].AsBool(false);
			consolidationRange = taskConfig["consolidationRange"].AsFloat(40f);
			// Movement speed when near or far away.
			moveSpeedNear = taskConfig["moveSpeed"].AsFloat(0.006f);
			moveSpeedFarAway = taskConfig["moveSpeedFarAway"].AsFloat(0.04f);
			// Movement animation when near or far.
			moveNearAnimation = taskConfig["moveNearAnimation"].AsString("Walk");
			moveFarAnimation = taskConfig["moveFarAnimation"].AsString("Run");
			BuildConsolidationTable(taskConfig);
			allowTeleport = taskConfig["allowTeleport"].AsBool(true);
			teleportAfterRange = taskConfig["teleportAfterRange"].AsFloat(30f);
		}

		private void BuildConsolidationTable(JsonObject taskConfig) {
			if (taskConfig["consolidationEntityCodes"] != null) {
				string[] array = taskConfig["consolidationEntityCodes"].AsArray(new string[0]);
				List<string> list = new List<string>();
				foreach (string text in array) {
					if (text.EndsWith("*")) {
						list.Add(text.Substring(0, text.Length - 1));
					} else {
						consolidationEntitiesByCodeExact.Add(text);
					}
				}
				consolidationEntitiesByCodePartial = list.ToArray();
			}
		}

		public override bool ShouldExecute() {
			// Initialize if values aren't set.
			if (thisEnt == null) {
				thisEnt = entity as EntityArcher;
			}
			// Only execute IF the entity has an owner, the owner is online, the owner is within range, and the entity is set to follow as an active order.
			if (thisEnt.ownerUID != null) {
				if (world.AllOnlinePlayers.Contains(thisEnt.ownerINT)) {
					if (entity.ServerPos.SquareDistanceTo(leaderEnt.ServerPos.XYZ) < maxDistance * maxDistance) {
						if (entity.Attributes.GetAsString("currentOrders") == "FOLLOW") {
							return true;
						}
					}
				}
			}
			return false;
		}

		public override void StartExecute() {
			base.StartExecute();
			var bh = entity.GetBehavior<EntityBehaviorControlledPhysics>();
			stepHeight = bh == null ? 0.6f : bh.stepHeight;
			PlayBestMoveAnimation();
			pathTraverser.WalkTowards(leaderEnt.ServerPos.XYZ, GetBestMoveSpeed(), leaderEnt.SelectionBox.XSize + 0.2f, OnGoalReached, OnStuck);
			targetOffset.Set(entity.World.Rand.NextDouble() * 2 - 1, 0, entity.World.Rand.NextDouble() * 2 - 1);
			stuck = false;
			stopNow = false;
		}

		protected float GetBestMoveSpeed() {
			double distSqr = entity.ServerPos.SquareDistanceTo(leaderEnt.ServerPos.XYZ);
			// If the distance between points is far away then speed up and run.
			if (distSqr > (maxDistance * 1.5) * (maxDistance * 1.5)) {
				return moveSpeedFarAway;
			} else {
				return moveSpeedNear;
			}
		}

		protected void PlayBestMoveAnimation() {
			double distSqr = entity.ServerPos.SquareDistanceTo(leaderEnt.ServerPos.XYZ);
			if (distSqr > (maxDistance * 1.5) * (maxDistance * 1.5)) {
				if (moveFarAnimation != null) {
					entity.AnimManager.StartAnimation(new AnimationMetaData() { Animation = moveFarAnimation, Code = moveFarAnimation }.Init());
				}
				if (moveNearAnimation != null) {
					entity.AnimManager.StopAnimation(moveNearAnimation);
				}
			} else {
				if (moveNearAnimation != null) {
					entity.AnimManager.StartAnimation(new AnimationMetaData() { Animation = moveNearAnimation, Code = moveNearAnimation }.Init());
				}
				if (moveFarAnimation != null) {
					entity.AnimManager.StopAnimation(moveFarAnimation);
				}
			}
		}

		Vec3d steeringVec = new Vec3d();
		public override bool ContinueExecute(float dt) {
			if (leaderEnt == null) {
				return false;
			} else {
				double x =  + targetOffset.X;
				double y = leaderEnt.ServerPos.Y;
				double z = leaderEnt.ServerPos.Z + targetOffset.Z;

				steeringVec.X = x;
				steeringVec.Y = y;
				steeringVec.Z = z;
				steeringVec = UpdateSteering(steeringVec);

				Vec3d herdLeaderPos = leaderEnt.ServerPos.XYZ;
				Vec3d herdLeaderPosClamped = AiUtility.ClampPositionToGround(world, herdLeaderPos, 5);

				pathTraverser.CurrentTarget.X = herdLeaderPosClamped.X;
				pathTraverser.CurrentTarget.Y = herdLeaderPosClamped.Y;
				pathTraverser.CurrentTarget.Z = herdLeaderPosClamped.Z;

				float size = leaderEnt.SelectionBox.XSize;

				if (leaderEnt.Swimming || leaderEnt.FeetInLiquid) {
					PlayBestMoveAnimation();
					pathTraverser.WalkTowards(herdLeaderPosClamped, GetBestMoveSpeed(), size + 0.2f, OnGoalReached, OnStuck);
				} else if ((!leaderEnt.Swimming && !leaderEnt.FeetInLiquid) && herdLeaderSwimmingLastFrame) {
					PlayBestMoveAnimation();
					pathTraverser.NavigateTo_Async(herdLeaderPosClamped, GetBestMoveSpeed(), size + 0.2f, OnGoalReached, OnStuck, OnPathFailed, 5000);
				}
				float distSqr = entity.ServerPos.SquareDistanceTo(x, y, z);
				if (distSqr < arriveDistance * arriveDistance) {
					pathTraverser.Stop();
					return false;
				}
				if (stuck && allowTeleport && distSqr > teleportAfterRange * teleportAfterRange) {
					AiUtility.TryTeleportToEntity(entity, leaderEnt);
				}
				herdLeaderSwimmingLastFrame = leaderEnt.Swimming || leaderEnt.FeetInLiquid;
				return !stuck && !stopNow && pathTraverser.Active && leaderEnt != null && leaderEnt.Alive;
			}
		}

		public virtual bool CanJoinThisEntityInHerd(EntityAgent herdMember) {
			if (!herdMember.Alive || !herdMember.IsInteractable || herdMember.EntityId == entity.EntityId || herdMember.HerdId == entity.HerdId) {
				return false;
			}
			if (consolidationEntitiesByCodeExact.Contains(herdMember.Code.Path)) {
				return true;
			}
			for (int i = 0; i < consolidationEntitiesByCodePartial.Length; i++) {
				if (herdMember.Code.Path.StartsWithFast(consolidationEntitiesByCodePartial[i])) {
					return true;
				}
			}
			return false;
		}

		public override void FinishExecute(bool cancelled) {
			base.FinishExecute(cancelled);
			pathTraverser.Stop();
			if (moveFarAnimation != null) {
				entity.AnimManager.StopAnimation(moveFarAnimation);
			}
			if (moveNearAnimation != null) {
				entity.AnimManager.StopAnimation(moveNearAnimation);
			}
		}

		protected void OnStuck() {
			stuck = true;
			if (allowTeleport) {
				AiUtility.TryTeleportToEntity(entity, leaderEnt);
			}
			pathTraverser.Stop();
		}

		public void OnPathFailed() {
			stopNow = true;
			if (allowTeleport) {
				AiUtility.TryTeleportToEntity(entity, leaderEnt);
			}
			pathTraverser.Stop();
		}

		protected void OnGoalReached() {
			pathTraverser.Stop();
		}

		
		private Vec3d UpdateSteering(Vec3d steerTarget) {
			Vec3d tmpVec = new Vec3d();
			float yaw = (float)Math.Atan2(entity.ServerPos.X - steerTarget.X, entity.ServerPos.Z - steerTarget.Z);
			// Simple steering behavior
			tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			tmpVec.Ahead(0.9, 0, yaw - GameMath.PI / 2);
			// Running into wall?
			if (Traversable(tmpVec)) {
				steerTarget.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10, 0, yaw - GameMath.PI / 2);
				return steerTarget;
			}
			// Try 90 degrees left
			tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			tmpVec.Ahead(0.9, 0, yaw - GameMath.PI);
			if (Traversable(tmpVec)) {
				steerTarget.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10, 0, yaw - GameMath.PI);
				return steerTarget;
			}
			// Try 90 degrees right
			tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			tmpVec.Ahead(0.9, 0, yaw);
			if (Traversable(tmpVec)) {
				steerTarget.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10, 0, yaw);
				return steerTarget;
			}
			// Run towards target o.O
			tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			tmpVec.Ahead(0.9, 0, -yaw);
			steerTarget.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10, 0, -yaw);
			return steerTarget;
		}

		bool Traversable(Vec3d pos) {
			return !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, pos, false);
		}

		public override bool Notify(string key, object data) {
			if (!entity.Alive) {
				return false;
			} else if (key == "haltMovement") {
				// If another task has requested we halt, stop moving to herd leader.
				if (entity == (Entity)data) {
					stopNow = true;
					return true;
				}
			}
			return false;
		}
	}
}