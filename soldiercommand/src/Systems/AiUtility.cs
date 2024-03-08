using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace SoldierCommand {
	public static class AiUtility {
		private const float ALWAYS_ALLOW_TELEPORT_BEYOND_RANGE_FROM_PLAYER = 80;
		private const float BLOCK_TELEPORT_AFTER_COMBAT_DURATION = 30000;

		public struct EntityTargetPairing {
			Entity _entityTargeting;
			Entity _targetEntity;
			Vec3d _lastPathUpdatePos = null;
			Vec3d _lastKnownPos = null;
			Vec3d _lastKnownMotion = null;
			public Entity entityTargeting { get { return _entityTargeting; } private set { _entityTargeting = value; } }
			public Entity targetEntity { get { return _targetEntity; } set { _targetEntity = value; } }
			public Vec3d lastPathUpdatePos { get { return _lastPathUpdatePos; } private set { _lastPathUpdatePos = value; } }
			public Vec3d lastKnownPos { get { return _lastKnownPos; } set { _lastKnownPos = value; } }
			public Vec3d lastKnownMotion { get { return _lastKnownMotion; } set { _lastKnownMotion = value; } }

			public EntityTargetPairing(Entity entityTargeting, Entity targetEntity, Vec3d lastPathUpdatePos, Vec3d lastKnownPos, Vec3d lastKnownMotion) {
				_entityTargeting = entityTargeting;
				_targetEntity = targetEntity;
				_lastPathUpdatePos = lastPathUpdatePos;
				_lastKnownPos = lastKnownPos;
				_lastKnownMotion = lastKnownMotion == null ? null : lastKnownMotion.Clone();
			}
		}

		private static Vec3d FindDecentTeleportPos(Entity entityToTeleport, Vec3d teleportLocation) {
			var rnd = entityToTeleport.World.Rand;
			Vec3d pos = new Vec3d();
			Cuboidf collisionBox = entityToTeleport.CollisionBox;
			int[] yTestOffsets = { 0, -1, 1, -2, 2, -3, 3 };
			for (int i = 0; i < 3; i++) {
				double randomXOffset = rnd.NextDouble() * 10 - 5;
				double randomYOffset = rnd.NextDouble() * 10 - 5;
				for (int j = 0; j < yTestOffsets.Length; j++) {
					int yAxisOffset = yTestOffsets[j];
					pos.Set(teleportLocation.X + randomXOffset, teleportLocation.Y + yAxisOffset, teleportLocation.Z + randomYOffset);
					// Test if this location is free and clear.
					if (!entityToTeleport.World.CollisionTester.IsColliding(entityToTeleport.World.BlockAccessor, collisionBox, pos, false)) {
						// POSSIBLE PERFORMANCE HAZARD!!! This call is effectively 2 X (3 X 7) traces per player if it fails. That's way too much!
						// If players can't see the entity's foot position.
						if (!AiUtility.CanAnyPlayerSeePos(pos, ALWAYS_ALLOW_TELEPORT_BEYOND_RANGE_FROM_PLAYER, entityToTeleport.World)) {
							// If players can't see the entity's eye position.
							if (!AiUtility.CanAnyPlayerSeePos(pos.Add(0, entityToTeleport.LocalEyePos.Y, 0), ALWAYS_ALLOW_TELEPORT_BEYOND_RANGE_FROM_PLAYER, entityToTeleport.World)) {
								return pos;
							}
						}
					}
				}
			}
			return null;
		}

		public static Vec3d GetCenterMass(Entity ent) {
			if (ent.SelectionBox.Empty) {
				return ent.SidedPos.XYZ;
			} else {
				float heightOffset = ent.SelectionBox.Y2 - ent.SelectionBox.Y1;
				return ent.SidedPos.XYZ.Add(0, heightOffset, 0);
			}
		}

		public static bool LocationInLiquid(IWorldAccessor world, Vec3d pos) {
			BlockPos blockPos = pos.AsBlockPos;
			Block block = world.BlockAccessor.GetBlock(blockPos);
			// If the block isn't null then return with checking if its liquid or not.
			if (block != null) {
				return block.BlockMaterial == EnumBlockMaterial.Liquid;
			} else {
				return false;
			}
		}

		public static Vec3d ClampPositionToGround(IWorldAccessor world, Vec3d startingPos, int maxBlockDistance) {
			BlockPos posAsBlockPos = startingPos.AsBlockPos;
			BlockPos previousCheckPos = posAsBlockPos.Copy();
			BlockPos currentCheckPos = posAsBlockPos.Copy();
			Block currentBlock = world.BlockAccessor.GetBlock(currentCheckPos);
			// Return if startingPos is null with what we have.
			if (currentBlock == null) {
				return startingPos;
			} else if (IsPositionInSolid(world, startingPos.AsBlockPos)) {
				return PopPositionAboveGround(world, startingPos, maxBlockDistance);
			} else {
				int groundCheckTries = 0;
				while (maxBlockDistance > groundCheckTries) {
					currentCheckPos = previousCheckPos.DownCopy();
					currentBlock = world.BlockAccessor.GetBlock(currentCheckPos);
					// Check Block Below us.
					if (currentBlock != null) {
						if (IsPositionInSolid(world, currentCheckPos)) {
							return new Vec3d(startingPos.X, previousCheckPos.Y, startingPos.Z);
						}
					}
					previousCheckPos = currentCheckPos;
					groundCheckTries++;
				}
				return startingPos;
			}
		}

		public static Vec3d PopPositionAboveGround(IWorldAccessor world, Vec3d startingPos, int maxBlockDistance) {
			BlockPos posAsBlockPos = startingPos.AsBlockPos;
			BlockPos previousCheckPos = posAsBlockPos.Copy();
			BlockPos currentCheckPos = posAsBlockPos.Copy();
			Block currentBlock = world.BlockAccessor.GetBlock(currentCheckPos);
			// Our starting point is in solid!
			if (currentBlock == null || !IsPositionInSolid(world, startingPos.AsBlockPos)) {
				return startingPos;
			} else {
				int groundCheckTries = 0;
				while (maxBlockDistance > groundCheckTries) {
					currentCheckPos = previousCheckPos.UpCopy();
					currentBlock = world.BlockAccessor.GetBlock(currentCheckPos);
					// Check Block Below us.
					if (currentBlock != null) {
						if (!IsPositionInSolid(world, currentCheckPos)) {
							return new Vec3d(startingPos.X, currentCheckPos.Y, startingPos.Z);
						}
					}
					previousCheckPos = currentCheckPos;
					groundCheckTries++;
				}
			}
			return startingPos;
		}

		public static Vec3d GetAiForwardViewVectorWithPitchTowardsPoint(Entity ent, Vec3d pitchPoint) {
			Vec3d entEyePos = ent.ServerPos.XYZ.Add(0, ent.LocalEyePos.Y, 0);
			double opposite = (pitchPoint.Y - entEyePos.Y);
			int dirScalar = opposite < 0 ? -1 : 1;
			double oppositeSqr = (opposite * opposite) * dirScalar;
			Vec3d dirFromEntToPoint2D = new Vec3d(pitchPoint.X - entEyePos.X, 0, pitchPoint.Z - entEyePos.Z);
			// Try to save the square
			double adjacentSqr = dirFromEntToPoint2D.LengthSq();
			double pitch = Math.Atan2(oppositeSqr, adjacentSqr);
			Vec3d eyePos = ent.ServerPos.XYZ.Add(0, ent.LocalEyePos.Y, 0);
			Vec3d aheadPos = eyePos.AheadCopy(1, pitch, ent.ServerPos.Yaw + (90 * (Math.PI / 180)));
			return (aheadPos - eyePos).Normalize();
		}

		public static Vec3d GetEntityForwardViewVector(Entity ent, Vec3d pitchPoint) {
			if (ent is EntityPlayer) {
				return GetPlayerForwardViewVector(ent);
			}
			return GetAiForwardViewVectorWithPitchTowardsPoint(ent, pitchPoint);
		}

		public static Vec3d GetPlayerForwardViewVector(Entity player) {
			Debug.Assert(player is EntityPlayer);
			Vec3d playerEyePos = player.ServerPos.XYZ.Add(0, player.LocalEyePos.Y, 0);
			Vec3d playerAheadPos = playerEyePos.AheadCopy(1, player.ServerPos.Pitch, player.ServerPos.Yaw);
			return (playerAheadPos - playerEyePos).Normalize();
		}

		public static EntityPlayer PlayerWithinRangeOfPos(Vec3d pos, float range, IWorldAccessor world) {
			IPlayer[] playersOnline = world.AllOnlinePlayers;
			foreach (IPlayer player in playersOnline) {
				EntityPlayer playerEnt = player.Entity;
				if (IsPlayerWithinRangeOfPos(playerEnt, pos, range)) {
					return playerEnt;
				}
			}
			// If can't find anything then just return null.
			return null;
		}

		private static BlockSelection blockSel = null;
		private static EntitySelection entitySel = null;
		private static Entity losTraceSourceEnt = null;
		private static Entity[] ignoreEnts = null;

		public static bool CanEntSeePos(Entity ent, Vec3d pos, float fov = AwarenessManager.DEFAULT_AI_VISION_FOV, Entity[] entsToIgnore = null) {
			blockSel = null;
			entitySel = null;
			losTraceSourceEnt = ent;
			ignoreEnts = entsToIgnore;
			Vec3d entEyePos = ent.ServerPos.XYZ.Add(0, ent.LocalEyePos.Y, 0);
			Vec3d entViewForward = GetEntityForwardViewVector(ent, pos);
			Vec3d entToPos = pos - entEyePos;
			entToPos = entToPos.Normalize();
			double maxViewDot = Math.Cos((fov / 2) * (Math.PI / 180));
			double dot = entViewForward.Dot(entToPos);

			if (dot > maxViewDot) {
				ent.World.RayTraceForSelection(entEyePos, pos, ref blockSel, ref entitySel, CanEntSeePos_BlockFilter, CanEntSeePos_EntityFilter);
				if (blockSel == null && entitySel == null) {
					return true;
				}
			}
			return false;
		}

		public static bool CanAnyPlayerSeePos(Vec3d pos, float autoPassRange, IWorldAccessor world, Entity[] ignoreEnts = null) {
			IPlayer[] playersOnline = world.AllOnlinePlayers;
			foreach (IPlayer player in playersOnline) {
				EntityPlayer playerEnt = player.Entity;

				if (IsPlayerWithinRangeOfPos(playerEnt, pos, autoPassRange)) {
					if (CanEntSeePos(playerEnt, pos, 160, ignoreEnts)) {
						return true;
					}
				}
			}
			return false;
		}

		public static bool CanAnyPlayerSeeMe(Entity ent, float autoPassRange, Entity[] ignoreEnts = null) {
			Vec3d myEyePos = ent.ServerPos.XYZ.Add(0, ent.LocalEyePos.Y, 0);
			return CanAnyPlayerSeePos(myEyePos, autoPassRange, ent.World, ignoreEnts);
		}

		public static bool IsPlayerWithinRangeOfPos(EntityPlayer player, Vec3d pos, float range) {
			double distSqr = player.ServerPos.XYZ.SquareDistanceTo(pos);
			return distSqr <= range * range;
		}

		public static bool IsAnyPlayerWithinRangeOfPos(Vec3d pos, float range, IWorldAccessor world) {
			IPlayer[] playersOnline = world.AllOnlinePlayers;
			foreach (IPlayer player in playersOnline) {
				EntityPlayer playerEnt = player.Entity;
				if (IsPlayerWithinRangeOfPos(playerEnt, pos, range)) {
					return true;
				}
			}
			return false;
		}

		public static bool IsInCombat(Entity ent) {
			if (ent is EntityPlayer) {
				return false;
			} else if (ent is EntityAgent) {
				// If we don't have AI Tasks, we cannot be in combat.
				if (!ent.HasBehavior<EntityBehaviorTaskAI>()) {
					return false;
				}

				AiTaskManager taskManager = ent.GetBehavior<EntityBehaviorTaskAI>().TaskManager;

				if (taskManager != null) {
					IAiTask[] activeTasks = taskManager.ActiveTasksBySlot;
					foreach (IAiTask task in activeTasks) {
						if (task is null) {
							continue;
						}
						if (task is AiTaskBaseTargetable) {
							AiTaskBaseTargetable baseTargetable = (AiTaskBaseTargetable)task;
							// If we are fleeing, we are in combat. (Not the same as morale)
							if (task is AiTaskFleeEntity) {
								return true;
							}
							// If not an agressive action.
							if (!baseTargetable.AggressiveTargeting) {
								continue;
							}
							// If we have a target entity and hostile intent, then we are in combat.
							if (baseTargetable.TargetEntity != null && baseTargetable.TargetEntity.Alive && !AreMembersOfSameGroup(ent, baseTargetable.TargetEntity)) {
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public static double GetLastTimeEntityInCombatMs(Entity ent) {
			double lastInCombatMs = ent.Attributes.GetDouble("lastTimeInCombatMs");
			if (lastInCombatMs > ent.World.ElapsedMilliseconds) {
				ent.Attributes.SetDouble("lastTimeInCombatMs", 0);
				lastInCombatMs = 0;
			}
			return lastInCombatMs;
		}

		public static void UpdateLastTimeEntityInCombatMs(Entity entSoldier) {
			// Set the time in combat (in milliseconds).
			entSoldier.Attributes.SetDouble("lastTimeInCombatMs", entSoldier.World.ElapsedMilliseconds);
		}

		public static void SetMasterHerdList(EntityArcher entSoldier, List<Entity> allyList) {
			// List of all alive allies.
			List<long> allyListEntityIds = new List<long>();
			// Add or remove agent entities if they exist or not to save space.
			foreach (EntityAgent agent in allyList) {
				if (agent != null) {
					allyListEntityIds.Add(agent.EntityId);
				} else {
					allyListEntityIds.Remove(agent.EntityId);
				}
			}

			// Write and serialize entity ID array.
			long[] allyEntityIds = allyListEntityIds.ToArray();
			entSoldier.Attributes.SetBytes("allyForces", SerializerUtil.Serialize(allyEntityIds));
		}

		public static List<Entity> GetMasterHerdList(EntityArcher entSoldier) {
			// List of active allies.
			List<Entity> allyForces = new List<Entity>();
			// Check if the entity has the members list for all their allies.
			if (entSoldier.Attributes.HasAttribute("allyForces")) {
				long[] allyEntityIds = SerializerUtil.Deserialize<long[]>(entSoldier.Attributes.GetBytes("allyForces"));
				foreach (long id in allyEntityIds) {
					Entity friend = entSoldier.World.GetEntityById(id);
					if (friend != null) {
						allyForces.Add(friend);
					}
				}
			}
			return allyForces;
		}

		public static bool AreMembersOfSameGroup(Entity ent1, Entity ent2) {
			// Check to see if target is player, another soldier, or something else entirely.
			if (ent1 is EntityArcher && ent2 is EntityPlayer) {
				int soldierGroup = ((EntityArcher)ent1).groupUID;
				int playersGroup = 0;
				// Check to see if the player group is even valid.
				if (((EntityPlayer)ent2).Player.GetGroups().Length > 0) {
					playersGroup = ((EntityPlayer)ent2).Player.GetGroups().ElementAt(0).GroupUid;
				}
				// So long as playersGroup AND soldierGroup aren't both 0 then go ahead. 
				if (soldierGroup != 0 && playersGroup != 0) {
					return soldierGroup == playersGroup;
				} else {
					return false;
				}
			} else if (ent1 is EntityArcher && ent2 is EntityArcher) {
				int soldierGroup = ((EntityArcher)ent1).groupUID;
				int entitysGroup = ((EntityArcher)ent2).groupUID;

				if (soldierGroup != 0 && entitysGroup != 0) {
					return soldierGroup == entitysGroup;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}

		public static void TryTeleportToEntity(Entity entityToTeleport, Entity targetEntity) {
			if (targetEntity == null) {
				return;
			}
			if (AiUtility.IsInCombat(entityToTeleport)) {
				return;
			}
			// We cannot teleport if we were recently in combat.
			if (entityToTeleport.World.ElapsedMilliseconds - AiUtility.GetLastTimeEntityInCombatMs(entityToTeleport) < BLOCK_TELEPORT_AFTER_COMBAT_DURATION) {
				return;
			}
			if (AiUtility.CanAnyPlayerSeeMe(entityToTeleport, ALWAYS_ALLOW_TELEPORT_BEYOND_RANGE_FROM_PLAYER)) {
				return;
			}
			if (AiUtility.CanAnyPlayerSeeMe(targetEntity, ALWAYS_ALLOW_TELEPORT_BEYOND_RANGE_FROM_PLAYER)) {
				return;
			}

			Vec3d teleportPos = FindDecentTeleportPos(entityToTeleport, targetEntity.ServerPos.XYZ);

			if (teleportPos != null) {
				entityToTeleport.TeleportTo(teleportPos);
			}
		}

		private static bool CanEntSeePos_BlockFilter(BlockPos pos, Block block) {
			// Leaves block visability
			if (block.BlockMaterial == EnumBlockMaterial.Leaves) {
				return false;
			}
			// Plants block visability
			if (block.BlockMaterial == EnumBlockMaterial.Plant) {
				return false;
			}
			return true;
		}

		private static bool CanEntSeePos_EntityFilter(Entity ent) {
			if (ent == losTraceSourceEnt) {
				return false;
			}
			if (ignoreEnts != null) {
				if (ignoreEnts.Contains(ent)) {
					return false;
				}
			}
			// AI can see through other AI.
			if (ent is EntityAgent) {
				return false;
			}
			return true;
		}

		public static bool IsPositionInSolid(IWorldAccessor world, BlockPos blockPos) {
			IBlockAccessor blockAccessor = world.BlockAccessor;
			Block blockAtPos = blockAccessor.GetBlock(blockPos);
			bool solid = blockAtPos.BlockMaterial != EnumBlockMaterial.Air && blockAtPos.BlockMaterial != EnumBlockMaterial.Liquid && blockAtPos.BlockMaterial != EnumBlockMaterial.Snow &&
				blockAtPos.BlockMaterial != EnumBlockMaterial.Plant && blockAtPos.BlockMaterial != EnumBlockMaterial.Leaves;

			if (solid) {
				bool confirmedSolid = false;
				foreach (BlockFacing facing in BlockFacing.ALLFACES) {
					if (blockAtPos.SideSolid[facing.Index] == true) {
						confirmedSolid = true;
						break;
					}

					BlockEntity blockEnt = blockAccessor.GetBlockEntity(blockPos);
					if (blockAtPos is BlockMicroBlock) {
						if (blockAccessor.GetBlockEntity(blockPos) is BlockEntityMicroBlock) {
							BlockEntityMicroBlock microBlockEnt = blockAccessor.GetBlockEntity(blockPos) as BlockEntityMicroBlock;
							if (microBlockEnt.sideAlmostSolid[facing.Index] == true) {
								confirmedSolid = true;
								break;
							}
						}
					}
				}
				solid = confirmedSolid;
			}
			return solid;
		}

		public static EntityPlayer FindPlayerEntity (string playerID, IWorldAccessor World) {
			IPlayer[] playersOnline = World.AllOnlinePlayers;
			// Go through all the players. If the owner is on, refresh to the owner's primary group.
			foreach (IPlayer player in playersOnline) {
				if (player.PlayerUID == playerID) {
					return player.Entity;
				}
			}
			return null;
		}

		public static bool EntityCodeInList(Entity ent, List<string> codes) {
			foreach (string code in codes) {
				if (ent.Code.Path == code) {
					return true;
				}
				if (ent.Code.Path.StartsWithFast(code)) {
					return true;
				}
			}
			return false;
		}

		public static bool EntityCodeInArray(Entity ent, string[] codes) {
			foreach (string code in codes) {
				if (ent.Code.Path == code) {
					return true;
				}
				if (ent.Code.Path.StartsWithFast(code)) {
					return true;
				}
			}
			return false;
		}
	}
}
