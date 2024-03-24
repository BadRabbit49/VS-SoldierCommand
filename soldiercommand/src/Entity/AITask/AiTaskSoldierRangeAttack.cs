using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;
using System.Collections.Generic;
using Vintagestory.API.Config;
using System.Drawing;
using System.Collections;

namespace SoldierCommand {
	public class AiTaskSoldierRangeAttack : AiTaskBaseTargetable {
		
		int durationMs = 2000;
		int releasesMs = 1000;
		int searchWaitMs = 7000;

		long lastSearchTotalMs;

		float accum = 0;
		float minDist = 4f;
		float maxDist = 18f;
		float minTurnAnglePerSec;
		float maxTurnAnglePerSec;
		float curTurnAnglePerSec;

		bool animsStarted;
		bool cancelAttack;
		bool didRenderSwitch;
		bool projectileFired;

		private EntityProperties projectileType;
		private AssetLocation drawingsound = null;
		private AssetLocation hittingsound = null;
		private AssetLocation ammoLocation = null;
		private AnimationMetaData aimBaseAnimMeta = null;
		private AnimationMetaData hitBaseAnimMeta = null;
		private AnimationMetaData aim0AnimMeta;
		private AnimationMetaData hit0AnimMeta;
		private AnimationMetaData aim1AnimMeta;
		private AnimationMetaData hit1AnimMeta;
		private AnimationMetaData aim2AnimMeta;
		private AnimationMetaData hit2AnimMeta;
		private AnimationMetaData aim3AnimMeta;
		private AnimationMetaData hit3AnimMeta;
		private AnimationMetaData aim4AnimMeta;
		private AnimationMetaData hit4AnimMeta;
		private AnimationMetaData aim5AnimMeta;
		private AnimationMetaData hit5AnimMeta;
		private AnimationMetaData aim6AnimMeta;
		private AnimationMetaData hit6AnimMeta;
		private AnimationMetaData aim7AnimMeta;
		private AnimationMetaData hit7AnimMeta;

		public AiTaskSoldierRangeAttack(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			// Due to how Vintage Story loads animation files, I have to do this individually. I know.
			aim0AnimMeta = new AnimationMetaData() {
				Animation = "BowDrawCrude".ToLowerInvariant(),
				Code = "BowDrawCrude".ToLowerInvariant(),
			}.Init();
			aim1AnimMeta = new AnimationMetaData() {
				Animation = "BowDrawSimple".ToLowerInvariant(),
				Code = "BowDrawSimple".ToLowerInvariant(),
			}.Init();
			aim2AnimMeta = new AnimationMetaData() {
				Animation = "BowDrawLong".ToLowerInvariant(),
				Code = "BowDrawLong".ToLowerInvariant(),
			}.Init();
			aim3AnimMeta = new AnimationMetaData() {
				Animation = "BowDrawRecurve".ToLowerInvariant(),
				Code = "BowDrawRecurve".ToLowerInvariant(),
			}.Init();
			aim4AnimMeta = new AnimationMetaData() {
				Animation = "SlingDraw1".ToLowerInvariant(),
				Code = "SlingDraw1".ToLowerInvariant(),
			}.Init();
			aim5AnimMeta = new AnimationMetaData() {
				Animation = "SlingDraw2".ToLowerInvariant(),
				Code = "SlingDraw2".ToLowerInvariant(),
			}.Init();
			aim6AnimMeta = new AnimationMetaData() {
				Animation = "ThrowDraw".ToLowerInvariant(),
				Code = "ThrowDraw".ToLowerInvariant(),
			}.Init();
			aim7AnimMeta = new AnimationMetaData() {
				Animation = "GunDraw".ToLowerInvariant(),
				Code = "GunDraw".ToLowerInvariant(),
			}.Init();
			// Same again for throw hit return anims.
			hit0AnimMeta = new AnimationMetaData() {
				Animation = "BowHit".ToLowerInvariant(),
				Code = "BowHit".ToLowerInvariant(),
			}.Init();
			hit1AnimMeta = new AnimationMetaData() {
				Animation = "BowHit".ToLowerInvariant(),
				Code = "BowHit".ToLowerInvariant(),
			}.Init();
			hit2AnimMeta = new AnimationMetaData() {
				Animation = "BowHit".ToLowerInvariant(),
				Code = "BowHit".ToLowerInvariant(),
			}.Init();
			hit3AnimMeta = new AnimationMetaData() {
				Animation = "BowHit".ToLowerInvariant(),
				Code = "BowHit".ToLowerInvariant(),
			}.Init();
			hit4AnimMeta = new AnimationMetaData() {
				Animation = "SlingHit1".ToLowerInvariant(),
				Code = "SlingHit1".ToLowerInvariant(),
			}.Init();
			hit5AnimMeta = new AnimationMetaData() {
				Animation = "SlingHit2".ToLowerInvariant(),
				Code = "SlingHit2".ToLowerInvariant(),
			}.Init();
			hit6AnimMeta = new AnimationMetaData() {
				Animation = "ThrowHit".ToLowerInvariant(),
				Code = "ThrowHit".ToLowerInvariant(),
			}.Init();
			hit7AnimMeta = new AnimationMetaData() {
				Animation = "BowHit".ToLowerInvariant(),
				Code = "BowHit".ToLowerInvariant(),
			}.Init();
		}

		public override bool ShouldExecute() {
			// Check for simple, easy-to-run searches like cooldown time, search time, and if the entity is in the water or not.
			if (cooldownUntilMs > entity.World.ElapsedMilliseconds) {
				return false;
			}
			if (lastSearchTotalMs + searchWaitMs > entity.World.ElapsedMilliseconds) {
				return false;
			}
			if (entity.Swimming) {
				return false;
			}
			if (targetEntity == entity) {
				targetEntity = null;
			}

			float range = maxDist;
			lastSearchTotalMs = entity.World.ElapsedMilliseconds;

			// Get the nearest targetable entity if no target exists already.
			if (targetEntity == null || !targetEntity.Alive) {
				targetEntity = partitionUtil.GetNearestInteractableEntity(entity.ServerPos.XYZ, range, (e) => IsTargetableEntity(e, range) && hasDirectContact(e, range, range / 2f) && SoldierUtility.CanTargetThis(entity, e));
			} else {
				targetEntity = null;
			}

			entity.Api.World.Logger.Notification("Entity is " + entity);
			entity.Api.World.Logger.Notification("Target is " + targetEntity);
			entity.Api.World.Logger.Notification("HasRanged " + HasRanged());
			entity.Api.World.Logger.Notification("HasArrows " + HasArrows());
			entity.Api.World.Logger.Notification("CanTarget " + SoldierUtility.CanTargetThis(entity, targetEntity));
			// Quickly check if the entity has a ranged weapon and arrows to proceed.
			return HasRanged() && HasArrows();
		}

		public override void StartExecute() {
			accum = 0;
			animsStarted = false;
			cancelAttack = false;
			didRenderSwitch = false;
			projectileFired = false;
			// Run through the list of preset variables for each item for balance.
			Random rnd = new Random();
			switch (WeapAnims()) {
				case 0: aimBaseAnimMeta = aim0AnimMeta; hitBaseAnimMeta = hit0AnimMeta; break;
				case 1: aimBaseAnimMeta = aim1AnimMeta; hitBaseAnimMeta = hit1AnimMeta; break;
				case 2: aimBaseAnimMeta = aim2AnimMeta; hitBaseAnimMeta = hit2AnimMeta; break;
				case 3: aimBaseAnimMeta = aim3AnimMeta; hitBaseAnimMeta = hit3AnimMeta; break;
				case 4: aimBaseAnimMeta = aim4AnimMeta; hitBaseAnimMeta = hit4AnimMeta; break;
				case 5: aimBaseAnimMeta = aim5AnimMeta; hitBaseAnimMeta = hit5AnimMeta; break;
				case 6: aimBaseAnimMeta = aim6AnimMeta; hitBaseAnimMeta = hit6AnimMeta; break;
				case 7: aimBaseAnimMeta = aim7AnimMeta; hitBaseAnimMeta = hit7AnimMeta; break;
			}
			// Get and initialize the item's attributes to the weapon.
			drawingsound = WeaponDefinitions.wepnAimAudio.Get(entity.RightHandItemSlot?.Itemstack?.Collectible?.Code);
			List<AssetLocation> hitAudio = WeaponDefinitions.wepnHitAudio.Get(entity.RightHandItemSlot?.Itemstack?.Collectible?.Code);
			hittingsound = hitAudio[rnd.Next(0, hitAudio.Count - 1)];
			ammoLocation = (entity as EntityArcher)?.AmmoItemSlot?.Itemstack?.Collectible?.Code;
			// Start switching the renderVariant to change to aiming.
			entity.RightHandItemSlot?.Itemstack?.Attributes?.SetInt("renderVariant", 1);
			entity.RightHandItemSlot?.MarkDirty();
			// Get whatever the asset entity type is based on the item's code path.
			projectileType = entity.World.GetEntityType(ammoLocation);
			entity.Api.World.Logger.Notification("The projectileType is: " + projectileType);
			entity.Api.World.Logger.Notification("The code for the shot: " + ammoLocation.ToString());
			if (entity.Properties.Server?.Attributes != null) {
				ITreeAttribute pathfinder = entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder");
				if (pathfinder != null) {
					minTurnAnglePerSec = pathfinder.GetFloat("minTurnAnglePerSec", 250);
					maxTurnAnglePerSec = pathfinder.GetFloat("maxTurnAnglePerSec", 450);
				}
			} else {
				minTurnAnglePerSec = 250;
				maxTurnAnglePerSec = 450;
			}
			curTurnAnglePerSec = minTurnAnglePerSec + (float)entity.World.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
			curTurnAnglePerSec *= GameMath.DEG2RAD * 50 * 0.02f;
		}

		public override bool ContinueExecute(float dt) {
			// Can't shoot if: there is no target, the attack has been cancelled, the shooter is swimming, or the target is too close!
			if (targetEntity == null || cancelAttack || entity.Swimming) {
				return false;
			}
			if (entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos.XYZ) <= minDist * minDist) {
				Vec3d targetPos = new Vec3d();
				updateTargetPosFleeMode(targetPos);
				pathTraverser.WalkTowards(targetPos, 0.035f, targetEntity.SelectionBox.XSize + 0.2f, OnGoalReached, OnStuck);
				pathTraverser.CurrentTarget.X = targetPos.X;
				pathTraverser.CurrentTarget.Y = targetPos.Y;
				pathTraverser.CurrentTarget.Z = targetPos.Z;
				pathTraverser.Retarget();
			}

			Vec3f targetVec = targetEntity.ServerPos.XYZFloat.Sub(entity.ServerPos.XYZFloat);
			targetVec.Set((float)(targetEntity.ServerPos.X - entity.ServerPos.X), (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y), (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z));

			float desiredYaw = (float)Math.Atan2(targetVec.X, targetVec.Z);
			float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);

			entity.ServerPos.Yaw += GameMath.Clamp(yawDist, -curTurnAnglePerSec * dt, curTurnAnglePerSec * dt);
			entity.ServerPos.Yaw = entity.ServerPos.Yaw % GameMath.TWOPI;
			if (Math.Abs(yawDist) > 0.02) {
				return true;
			}
			// Start animations if not already doing so.
			if (!animsStarted) {
				animsStarted = true;
				aimBaseAnimMeta.EaseInSpeed = 1f;
				aimBaseAnimMeta.EaseOutSpeed = 1f;
				entity.AnimManager.StartAnimation(aimBaseAnimMeta);
				if (drawingsound != null) {
					entity.World.PlaySoundAt(drawingsound, entity, null, false);
				}
			}

			accum += dt;
			
			if (!didRenderSwitch && accum > durationMs / 2000f) {
				entity.RightHandItemSlot?.Itemstack?.Attributes?.SetInt("renderVariant", 3);
				entity.RightHandItemSlot.MarkDirty();
				didRenderSwitch = true;
			}
			// Do after aiming time is finished.
			entity.Api.World.Logger.Notification("Did RenderSwitch: " + didRenderSwitch);
			entity.Api.World.Logger.Notification("Projectile Fired: " + !projectileFired);
			entity.Api.World.Logger.Notification("Over MS Duration: " + (accum > releasesMs / 1000f));
			entity.Api.World.Logger.Notification("Clear sightlines: " + !EntityInTheWay());
			if (accum > releasesMs / 1000f && !projectileFired && !EntityInTheWay()) {
				entity.Api.World.Logger.Notification("SUCCESS!");
				projectileFired = true;
				EntityProjectile projectile = (EntityProjectile)entity.World.ClassRegistry.CreateEntity(projectileType);
				projectile.FiredBy = entity;
				projectile.Damage = GetDamage();
				projectile.ProjectileStack = new ItemStack(entity.World.GetItem(ammoLocation));
				projectile.DropOnImpactChance = DropRates();
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
				// Don't play anything when the hittingSound is incorrectly set.
				if (hittingsound != null) {
					entity.World.PlaySoundAt(hittingsound, entity, null, false);
				}
				entity.AnimManager.StopAnimation(aimBaseAnimMeta.Code);
				if (hitBaseAnimMeta != null) {
					hitBaseAnimMeta.EaseInSpeed = 1f;
					hitBaseAnimMeta.EaseOutSpeed = 1f;
					entity.AnimManager.StartAnimation(hitBaseAnimMeta);
				}
			}
			return accum < durationMs / 1000f && !cancelAttack;
		}

		public override void FinishExecute(bool cancelled) {
			base.FinishExecute(cancelled);
			if (aimBaseAnimMeta != null && entity.AnimManager.IsAnimationActive(aimBaseAnimMeta.Code)) {
				entity.AnimManager.StopAnimation(aimBaseAnimMeta.Code);
			}
			if (hitBaseAnimMeta != null && entity.AnimManager.IsAnimationActive(hitBaseAnimMeta.Code)) {
				entity.AnimManager.StopAnimation(hitBaseAnimMeta.Code);
			}
			if (projectileFired) {
				if (!entity.Api.World.Config.GetAsBool("InfiniteAmmos")) {
					(entity as EntityArcher).AmmoItemSlot.TakeOut(1);
					(entity as EntityArcher).AmmoItemSlot.MarkDirty();
				}
				entity.RightHandItemSlot?.Itemstack?.Attributes?.SetInt("renderVariant", 0);
				entity.RightHandItemSlot?.MarkDirty();
			}
			return;
		}
		
		public override void OnEntityHurt(DamageSource source, float damage) {
			base.OnEntityHurt(source, damage);
			cancelAttack = true;
		}

		public void OnAllyAttacked(Entity byEntity) {
			if (byEntity != entity) {
				if (targetEntity == null || !targetEntity.Alive) {
					targetEntity = byEntity;
				}
			}
		}

		private void OnStuck() {
			updateTargetPosFleeMode(entity.Pos.XYZ);
		}

		private void OnGoalReached() {
			pathTraverser.Retarget();
		}

		private float GetDamage() {
			if (HasRanged() && HasArrows()) {
				return WeaponDefinitions.WeaponDamage.Get(entity.RightHandItemSlot?.Itemstack?.Collectible?.Code) + WeaponDefinitions.BulletDamage.Get((entity as EntityArcher)?.AmmoItemSlot?.Itemstack?.Collectible?.Code);
			} else {
				return 3f;
			}
		}

		private float DropRates() {
			return 1 - (entity as EntityArcher).AmmoItemSlot.Itemstack.Collectible.Attributes["breakChanceOnImpact"].AsFloat();
		}

		private int WeapAnims() {
			return WeaponDefinitions.weaponsAnims.Get(entity.RightHandItemSlot?.Itemstack?.Collectible?.Code);
		}

		private bool HasRanged() {
			return WeaponDefinitions.AcceptedRange.Contains(entity.RightHandItemSlot?.Itemstack?.Collectible?.Code);
		}

		private bool HasArrows() {
			return WeaponDefinitions.AcceptedAmmos.Contains((entity as EntityArcher)?.AmmoItemSlot?.Itemstack?.Collectible?.Code);
		}

		private bool EntityInTheWay() {
			EntitySelection entitySel = new EntitySelection();
			BlockSelection blockSel = new BlockSelection();

			entity.World.RayTraceForSelection(entity.ServerPos.XYZ.AddCopy(entity.LocalEyePos), targetEntity?.ServerPos?.XYZ.AddCopy(targetEntity?.LocalEyePos), ref blockSel, ref entitySel);
			// Make sure the target isn't obstructed by other entities, but if it IS then make sure it's okay to hit them.
			if (entitySel?.Entity != targetEntity) {
				// Determine if the entity in the way is a friend or foe, if they're an enemy then disregard and shoot anyway.
				if (entitySel?.Entity is EntityPlayer || entitySel?.Entity is EntityArcher) {
					return SoldierUtility.CanTargetThis(entity, entitySel.Entity);
				}
				// Fuck all drifters, locusts, and bells, a shot well placed I say. Infact, switch targets to kill IT.
				if (entitySel?.Entity is EntityDrifter || entitySel?.Entity is EntityLocust || entitySel?.Entity is EntityBell) {
					targetEntity = entitySel.Entity;
					return false;
				}
				// For the outlaw mod specifically. These bozos are just as bad. So don't worry about it, reprioritize.
				if (entitySel?.Entity?.Class == "EntityOutlaw") {
					targetEntity = entitySel.Entity;
					return false;
				}
				return true;
			}
			return false;
		}
	}
}