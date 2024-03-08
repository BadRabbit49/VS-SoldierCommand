using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;
namespace SoldierCommand {
	public class AiTaskSoldierRangeAttack : AiTaskBaseTargetable {
		int durationMs;
		int releaseAtMs;
		long lastSearchTotalMs;

		float minVertDist = 2f;
		float minDist = 3f;
		float maxDist = 15f;
		float accum = 0;
		float damage;

		protected int searchWaitMs = 7000;

		bool animStarted;
		bool didThrow;
		bool didRenderswitch;

		float minTurnAnglePerSec;
		float maxTurnAnglePerSec;
		float curTurnRadPerSec;

		protected EntityProperties projectileType;
		protected AssetLocation drawingsound;
		protected AssetLocation hittingsound;

		AnimationMetaData aimAnimMeta;
		AnimationMetaData hitAnimMeta;

		public AiTaskSoldierRangeAttack(EntityAgent entity) : base(entity) { }
		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			durationMs = taskConfig["durationMs"].AsInt(1500);
			releaseAtMs = taskConfig["releaseAtMs"].AsInt(1000);
			minDist = taskConfig["minDist"].AsFloat(3f);
			minVertDist = taskConfig["minVertDist"].AsFloat(2f);
			maxDist = taskConfig["maxDist"].AsFloat(15f);

			if (taskConfig["drawingsound"].Exists) {
				drawingsound = new AssetLocation(taskConfig["drawingsound"].AsString());
			}
			if (taskConfig["hittingsound"].Exists) {
				hittingsound = new AssetLocation(taskConfig["hittingsound"].AsString());
			}
			if (taskConfig["aimanimation"].Exists) {
				aimAnimMeta = new AnimationMetaData() {
					Animation = taskConfig["aimanimation"].AsString()?.ToLowerInvariant(),
					Code = taskConfig["aimanimation"].AsString()?.ToLowerInvariant(),
				}.Init();
			}
			if (taskConfig["hitanimation"].Exists) {
				hitAnimMeta = new AnimationMetaData() {
					Code = taskConfig["hitanimation"].AsString()?.ToLowerInvariant(),
					Animation = taskConfig["hitanimation"].AsString()?.ToLowerInvariant(),
				}.Init();
			}
		}

		public override bool ShouldExecute() {
			if (cooldownUntilMs > entity.World.ElapsedMilliseconds) {
				return false;
			}
			if (lastSearchTotalMs + searchWaitMs < entity.World.ElapsedMilliseconds && targetEntity?.Alive != true || lastSearchTotalMs + searchWaitMs * 5 < entity.World.ElapsedMilliseconds) {
				float range = maxDist;
				lastSearchTotalMs = entity.World.ElapsedMilliseconds;
				targetEntity = partitionUtil.GetNearestInteractableEntity(entity.ServerPos.XYZ, range, (e) => IsTargetableEntity(e, range * 4) && hasDirectContact(e, range * 4, range / 2f));
			}
			return targetEntity?.Alive == true;
		}

		public override void StartExecute() {
			EntityArcher thisEnt = (entity as EntityArcher);
			if (thisEnt.RightHandItemSlot != null && !thisEnt.RightHandItemSlot.Empty) {
				thisEnt.RightHandItemSlot?.Itemstack?.Attributes?.SetInt("renderVariant", 1);
				thisEnt.RightHandItemSlot.MarkDirty();
				ItemStack stack = thisEnt.AmmoItemSlot.Itemstack;
				if (stack != null) {
					// Get whatever the asset entity type is based on the item's code path.
					projectileType = entity.World.GetEntityType(new AssetLocation(stack.Item.Code.Path));
					if (stack.Item is ItemArrow) {
						// Regular arrows, get the actual type's damage modifier.
						damage = stack.Collectible.Attributes["damage"].AsFloat(0);
					} else if (stack.Item.Attributes["projectile"].Exists) {
						// For Maltiez's firearms mod, can't extract damage out of bullets nicely so here we are.
						if (stack.Item.Code.Path == "maltiezfirearms:slug") {
							damage = 30;
						} else if (stack.Item.Code.Path == "maltiezfirearms:bullet") {
							damage = 14;
						}
					}
				} else {
					return;
				}
			}
			
			accum = 0;
			didThrow = false;
			animStarted = false;
			didRenderswitch = false;

			if (entity?.Properties.Server?.Attributes != null) {
				ITreeAttribute pathfinder = entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder");
				if (pathfinder != null) {
					minTurnAnglePerSec = pathfinder.GetFloat("minTurnAnglePerSec", 250);
					maxTurnAnglePerSec = pathfinder.GetFloat("maxTurnAnglePerSec", 450);
				}
			} else {
				minTurnAnglePerSec = 250;
				maxTurnAnglePerSec = 450;
			}
			curTurnRadPerSec = minTurnAnglePerSec + (float)entity.World.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
			curTurnRadPerSec *= GameMath.DEG2RAD * 50 * 0.02f;
		}

		public override bool ContinueExecute(float dt) {
			Vec3f targetVec = targetEntity.ServerPos.XYZFloat.Sub(entity.ServerPos.XYZFloat);
			targetVec.Set((float)(targetEntity.ServerPos.X - entity.ServerPos.X), (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y), (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z));

			float desiredYaw = (float)Math.Atan2(targetVec.X, targetVec.Z);
			float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);

			entity.ServerPos.Yaw += GameMath.Clamp(yawDist, -curTurnRadPerSec * dt, curTurnRadPerSec * dt);
			entity.ServerPos.Yaw = entity.ServerPos.Yaw % GameMath.TWOPI;

			if (Math.Abs(yawDist) > 0.02) {
				return true;
			}
			if (aimAnimMeta != null && !animStarted) {
				animStarted = true;
				aimAnimMeta.EaseInSpeed = 1f;
				aimAnimMeta.EaseOutSpeed = 1f;
				entity.AnimManager.StartAnimation(aimAnimMeta);
				if (drawingsound != null) {
					entity.World.PlaySoundAt(drawingsound, entity, null, false);
				}
			}
			
			accum += dt;

			if (entity is EntityArcher && !didRenderswitch && accum > releaseAtMs / 2000f) {
				entity.RightHandItemSlot?.Itemstack?.Attributes?.SetInt("renderVariant", 3);
				entity.RightHandItemSlot.MarkDirty();
				didRenderswitch = true;
			}
			if (accum > releaseAtMs / 1000f && !didThrow && !EntityInTheWay()) {
				didThrow = true;
				EntityProjectile projectile = (EntityProjectile)entity.World.ClassRegistry.CreateEntity(projectileType);
				projectile.FiredBy = entity;
				projectile.Damage = damage;
				projectile.ProjectileStack = new ItemStack();
				projectile.DropOnImpactChance = 0;
				projectile.World = entity.World;
				Vec3d pos = entity.ServerPos.AheadCopy(0.5).XYZ.AddCopy(0, entity.LocalEyePos.Y, 0);
				Vec3d aheadPos = targetEntity.ServerPos.XYZ.AddCopy(0, targetEntity.LocalEyePos.Y, 0);
				double distf = Math.Pow(pos.SquareDistanceTo(aheadPos), 0.1);
				Vec3d velocity = (aheadPos - pos + new Vec3d(0, pos.DistanceTo(aheadPos) / 16, 0)).Normalize() * GameMath.Clamp(distf - 1f, 0.1f, 1f);
				// Set final projectile parameters, position, velocity, from point, and rotation.
				projectile.ServerPos.SetPos(entity.ServerPos.AheadCopy(0.5).XYZ.Add(0, entity.LocalEyePos.Y, 0));
				projectile.ServerPos.Motion.Set(velocity);
				projectile.Pos.SetFrom(projectile.ServerPos);
				projectile.SetRotation();
				// Spawn and fire the entity with given parameters.
				entity.World.SpawnEntity(projectile);
				if (hittingsound != null) {
					entity.World.PlaySoundAt(hittingsound, entity, null, false);
				}
				if (hitAnimMeta != null) {
					hitAnimMeta.EaseInSpeed = 1f;
					hitAnimMeta.EaseOutSpeed = 1f;
					entity.AnimManager.StartAnimation(hitAnimMeta);
				}
			}
			return accum < durationMs / 1000f;
		}

		public override void FinishExecute(bool cancelled) {
			base.FinishExecute(cancelled);
			if (entity is EntityArcher) {
				entity.RightHandItemSlot?.Itemstack?.Attributes?.SetInt("renderVariant", 0);
				entity.RightHandItemSlot.MarkDirty();
			}
		}

		public override bool IsTargetableEntity(Entity ent, float range, bool ignoreEntityCode = false) {
			if (ent == attackedByEntity && ent?.Alive == true) {
				return true;
			} else {
				return base.IsTargetableEntity(ent, range, ignoreEntityCode);
			}
		}

		private bool EntityInTheWay() {
			var entitySel = new EntitySelection();
			var blockSel = new BlockSelection();
			entity.World.RayTraceForSelection(entity.ServerPos.XYZ.AddCopy(entity.LocalEyePos), targetEntity.ServerPos.XYZ.AddCopy(targetEntity.LocalEyePos), ref blockSel, ref entitySel);
			return entitySel?.Entity != targetEntity;
		}

		public void OnAllyAttacked(Entity byEntity) {
			if (targetEntity == null || !targetEntity.Alive) {
				targetEntity = byEntity;
			}
		}
	}
}