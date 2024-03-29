﻿using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using HarmonyLib;

namespace SoldierCommand {
	public class AiTaskSoldierWanderAbout : AiTaskBase {
		public Vec3d MainTarget;

		bool done;
		float moveSpeed = 0.03f;
		float wanderChance = 0.02f;
		float maxHeight = 7f;
		float? preferredLightLevel;
		float targetDistance = 0.12f;

		NatFloat wanderRangeHorizontal = NatFloat.createStrongerInvexp(3, 40);
		NatFloat wanderRangeVertical = NatFloat.createStrongerInvexp(3, 10);


		public bool StayCloseToSpawn;
		public double MaxDistanceToSpawn;
		public Vec3d SpawnPosition;

		long lastTimeInRangeMs;
		int failedWanders;

		public float WanderRangeMul {
			get { return entity.Attributes.GetFloat("wanderRangeMul", 1); }
			set { entity.Attributes.SetFloat("wanderRangeMul", value); }
		}

		public int FailedConsecutivePathfinds {
			get { return entity.Attributes.GetInt("failedConsecutivePathfinds", 0); }
			set { entity.Attributes.SetInt("failedConsecutivePathfinds", value); }
		}

		public AiTaskSoldierWanderAbout(EntityAgent entity) : base(entity) { }

		public override void OnEntityLoaded() { }

		public override void OnEntitySpawn() {
			entity.Attributes.SetDouble("spawnX", entity.ServerPos.X);
			entity.Attributes.SetDouble("spawnY", entity.ServerPos.Y);
			entity.Attributes.SetDouble("spawnZ", entity.ServerPos.Z);
			SpawnPosition = entity.ServerPos.XYZ;
		}

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			SpawnPosition = new Vec3d(entity.Attributes.GetDouble("spawnX"), entity.Attributes.GetDouble("spawnY"), entity.Attributes.GetDouble("spawnZ"));
			float wanderRangeMin = 3, wanderRangeMax = 30;

			if (taskConfig["maxDistanceToSpawn"].Exists) {
				StayCloseToSpawn = true;
				MaxDistanceToSpawn = taskConfig["maxDistanceToSpawn"].AsDouble(10);
			}

			targetDistance = taskConfig["targetDistance"].AsFloat(0.12f);
			moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
			wanderChance = taskConfig["wanderChance"].AsFloat(0.015f);
			wanderRangeMin = taskConfig["wanderRangeMin"].AsFloat(3);
			wanderRangeMax = taskConfig["wanderRangeMax"].AsFloat(30);
			wanderRangeHorizontal = NatFloat.createInvexp(wanderRangeMin, wanderRangeMax);

			maxHeight = taskConfig["maxHeight"].AsFloat(7f);
			preferredLightLevel = taskConfig["preferredLightLevel"].AsFloat(-99);
			if (preferredLightLevel < 0) {
				preferredLightLevel = null;
			}
		}

		// Requirements:
		// - ✔ Try to not move a lot vertically
		// - ~~If cave habitat: Prefer caves~~
		// - ✔ If water habitat: Don't walk onto land
		// - ✔ Try not to fall from very large heights. Try not to fall from any large heights if entity has FallDamage
		// - ✔ Prefer preferredLightLevel
		// - ✔ If land habitat: Must be above a block the entity can stand on
		// - ✔ if failed searches is high, reduce wander range
		public Vec3d loadNextWanderTarget() {
			bool canFallDamage = entity.Properties.FallDamage;
			bool territorial = StayCloseToSpawn;
			int tries = 9;
			double W = 0;
			Vec4d bestTarget = null;
			BlockPos curTarget = new BlockPos(0);
			BlockPos tmpPos = new BlockPos(0);

			if (FailedConsecutivePathfinds > 10) {
				WanderRangeMul = Math.Max(0.1f, WanderRangeMul * 0.9f);
			} else {
				WanderRangeMul = Math.Min(1, WanderRangeMul * 1.1f);
				if (rand.NextDouble() < 0.05) {
					WanderRangeMul = Math.Min(1, WanderRangeMul * 1.5f);
				}
			}

			float wRangeMul = WanderRangeMul;
			double dx, dy, dz;

			if (rand.NextDouble() < 0.05) {
				wRangeMul *= 3;
			}
			while (tries-- > 0) {
				dx = wanderRangeHorizontal.nextFloat() * (rand.Next(2) * 2 - 1) * wRangeMul;
				dy = wanderRangeVertical.nextFloat() * (rand.Next(2) * 2 - 1) * wRangeMul;
				dz = wanderRangeHorizontal.nextFloat() * (rand.Next(2) * 2 - 1) * wRangeMul;
				curTarget.X = entity.ServerPos.AsBlockPos.X + (int)dx;
				curTarget.Y = entity.ServerPos.AsBlockPos.Y + (int)dy;
				curTarget.Z = entity.ServerPos.AsBlockPos.Z + (int)dz;
				W = 1;

				if (StayCloseToSpawn) {
					double distToEdge = curTarget.ToVec3d().SquareDistanceTo(SpawnPosition) / (MaxDistanceToSpawn * MaxDistanceToSpawn);
					// Prefer staying close to spawn.
					W = 1 - distToEdge;
				}
				Block waterorIceBlock;
				curTarget.Y = moveDownToFloor(curTarget);
				// No floor found.
				if (curTarget.Y < 0) {
					W = 0;
				} else {
					// Does not like water.
					waterorIceBlock = entity.World.BlockAccessor.GetBlock(curTarget, BlockLayersAccess.Fluid);
					if (waterorIceBlock.IsLiquid()) {
						W /= 2;
					}
					// Lets make a straight line plot to see if we would fall off a cliff.
					bool stop = false;
					bool willFall = false;
					float angleHor = (float)Math.Atan2(dx, dz) + GameMath.PIHALF;
					// Otherwise they are forever stuck if they stand over the edge.
					Vec3d target1BlockAhead = curTarget.ToVec3d().Ahead(1, 0, angleHor);
					Vec3d startAhead = entity.ServerPos.XYZ.Ahead(1, 0, angleHor);
					int prevY = (int)startAhead.Y;

					GameMath.BresenHamPlotLine2d((int)startAhead.X, (int)startAhead.Z, (int)target1BlockAhead.X, (int)target1BlockAhead.Z, (x, z) => {
						if (stop) {
							return;
						}

						double nowY = moveDownToFloor(curTarget);

						// Not more than 4 blocks down
						if (nowY < 0 || prevY - nowY > 4) {
							willFall = true;
							stop = true;
						}
						// Not more than 2 blocks up
						if (nowY - prevY > 2) {
							stop = true;
						}
						prevY = (int)nowY;
					});
					if (willFall) {
						W = 0;
					}
				}
				if (W > 0) {
					// Try to not hug the wall so much
					for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++) {
						BlockFacing face = BlockFacing.HORIZONTALS[i];
						if (entity.World.BlockAccessor.IsSideSolid((int)curTarget.X + face.Normali.X, (int)curTarget.Y, (int)curTarget.Z + face.Normali.Z, face.Opposite)) {
							W *= 0.5;
						}
					}
				}
				if (preferredLightLevel != null && W != 0) {
					tmpPos.Set(curTarget);
					int lightdiff = Math.Abs((int)preferredLightLevel - entity.World.BlockAccessor.GetLightLevel(tmpPos, EnumLightLevelType.MaxLight));
					W /= Math.Max(1, lightdiff);
				}
				if (bestTarget == null || W > bestTarget.W) {
					bestTarget = new Vec4d(curTarget.X, curTarget.Y, curTarget.Z, W);
					// We have a good enough target, no need for further tries.
					if (W >= 1.0) {
						break;
					}
				}
			}
			if (bestTarget.W > 0) {
				FailedConsecutivePathfinds = Math.Max(FailedConsecutivePathfinds - 3, 0);
				return bestTarget.XYZ;
			}
			FailedConsecutivePathfinds++;
			return null;
		}

		private int moveDownToFloor(BlockPos pos) {
			int tries = 5;
			while (tries-- > 0) {
				if (world.BlockAccessor.IsSideSolid(pos.X, pos.Y, pos.Z, BlockFacing.UP)) {
					return pos.Y + 1;
				}
				pos.Y--;
			}
			return -1;
		}

		public override bool ShouldExecute() {
			// If a wander failed (got stuck) initially greatly increase the chance of trying again, but eventually give up.
			if (rand.NextDouble() > (failedWanders > 0 ? (1 - wanderChance * 4 * failedWanders) : wanderChance)) {
				failedWanders = 0;
				return false;
			}

			double dist = entity.ServerPos.XYZ.SquareDistanceTo(SpawnPosition);
			if (StayCloseToSpawn) {
				long ellapsedMs = entity.World.ElapsedMilliseconds;
				if (dist > MaxDistanceToSpawn * MaxDistanceToSpawn) {
					// If after 2 minutes still not at spawn and no player nearby, teleport.
					if (ellapsedMs - lastTimeInRangeMs > 1000 * 60 * 2 && entity.World.GetNearestEntity(entity.ServerPos.XYZ, 15, 15, (e) => e is EntityPlayer) == null) {
						entity.TeleportTo(SpawnPosition);
					}
					MainTarget = SpawnPosition.Clone();
					return true;
				} else {
					lastTimeInRangeMs = ellapsedMs;
				}
			}
			MainTarget = loadNextWanderTarget();
			return MainTarget != null;
		}

		public override void StartExecute() {
			base.StartExecute();
			if (!entity.RightHandItemSlot.Empty) {
				WeaponAnims wepAnim = AnimationDefinitions.WeaponAnimations.Find(WeaponAnims => WeaponAnims.itemCode == entity.RightHandItemSlot?.Itemstack?.Collectible?.Code);
				// Animation should adjust to the entity's behavior and equipped weapon.
				animMeta = new AnimationMetaData {
					Code = wepAnim.walkAnim.ToLowerInvariant(),
					Animation = wepAnim.walkAnim.ToLowerInvariant(),
					AnimationSpeed = wepAnim.walkTime * ((float)entity.GetWalkSpeedMultiplier())
				}.Init();
				animMeta.EaseInSpeed = 1f;
				animMeta.EaseOutSpeed = 1f;
			}
			done = false;
			bool ok = pathTraverser.WalkTowards(MainTarget, moveSpeed, targetDistance, OnGoalReached, OnStuck);
		}

		public override bool ContinueExecute(float dt) {
			base.ContinueExecute(dt);
			// If we are a climber dude and encountered a wall, let's not try to get behind the wall.
			// We do that by removing the coord component that would make the entity want to walk behind the wall.
			if (entity.Controls.IsClimbing && entity.Properties.CanClimbAnywhere && entity.ClimbingIntoFace != null) {
				BlockFacing facing = entity.ClimbingIntoFace;
				if (Math.Sign(facing.Normali.X) == Math.Sign(pathTraverser.CurrentTarget.X - entity.ServerPos.X)) {
					pathTraverser.CurrentTarget.X = entity.ServerPos.X;
				}
				if (Math.Sign(facing.Normali.Y) == Math.Sign(pathTraverser.CurrentTarget.Y - entity.ServerPos.Y)) {
					pathTraverser.CurrentTarget.Y = entity.ServerPos.Y;
				}
				if (Math.Sign(facing.Normali.Z) == Math.Sign(pathTraverser.CurrentTarget.Z - entity.ServerPos.Z)) {
					pathTraverser.CurrentTarget.Z = entity.ServerPos.Z;
				}
			}
			// If the entity is close enough to the primary target then leave it there.
			if (MainTarget.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z) < 0.5) {
				pathTraverser.Stop();
				return false;
			}
			return !done;
		}

		public override void FinishExecute(bool cancelled) {
			base.FinishExecute(cancelled);
			if (animMeta != null && entity.AnimManager.IsAnimationActive(animMeta.Code)) {
				entity.AnimManager.StopAnimation(animMeta.Code);
			}
			if (cancelled) {
				pathTraverser.Stop();
			}
		}

		private void OnStuck() {
			done = true;
			failedWanders++;
		}

		private void OnGoalReached() {
			done = true;
			failedWanders = 0;
		}
	}
}