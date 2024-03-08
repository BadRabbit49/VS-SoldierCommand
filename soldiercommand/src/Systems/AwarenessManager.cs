using SoldierCommand;
using System.Collections.Generic;
using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SoldierCommand {
	public struct AwarenessData {
		private bool _isAware;
		private double _lastComputationTime;

		public AwarenessData(bool isAware, double lastComputationTime) {
			_isAware = isAware;
			_lastComputationTime = lastComputationTime;
		}

		public bool isAware {
			get { return _isAware; }
			set { _isAware = value; }
		}

		public double lastComputationTime {
			get { return _lastComputationTime; }
			set { _lastComputationTime = value; }
		}
	}

	public static class AwarenessManager {
		private static Dictionary<long, Dictionary<long, AwarenessData>> awarenessData = new Dictionary<long, Dictionary<long, AwarenessData>>();

		// NOTE: This value should be as high as we can make it without its effect being perceptable to players.
		private const float AWARENESS_STALE_AFTER_TIME_MS = 250f;

		public const float DEFAULT_AI_VISION_FOV = 120;
		public const float DEFAULT_AI_SCENT_WIND_FOV = 20;

		private const double AI_VISION_AWARENESS_SNEAK_MODIFIER = 0.20;
		private const double AI_VISION_AWARENESS_STANDNG_MODIFIER = 0.5;
		private const double AI_VISION_AWARENESS_WALK_MODIFIER = 1.0;
		private const double AI_VISION_AWARENESS_SPRINT_MODIFIER = 1.0;
		
		private static string[] alwaysIgnoreEntityCodes = { "butterfly", "chicken-baby", "chicken-hen", "chicken-rooster", "hare-baby", "hare-female", "hare-male", "boat", "salmon", "bass", "strawdummy", "armorstand" };

		public static void Init() {}

		public static void ShutdownCleanup() {
			foreach (Dictionary<long, AwarenessData> awarenessEntry in awarenessData.Values) {
				awarenessEntry.Clear();
			}

			awarenessData.Clear();
		}

		public static bool EntityHasAwarenessEntry(Entity ent) {
			return awarenessData.ContainsKey(ent.EntityId);
		}

		public static bool EntityHasAwarenessEntryForTargetEntity(Entity ent, Entity targetEnt) {
			return awarenessData[ent.EntityId].ContainsKey(targetEnt.EntityId);
		}

		public static bool EntityAwarenessEntryForTargetEntityIsStale(Entity ent, Entity targetEnt) {
			return ent.World.ElapsedMilliseconds > awarenessData[ent.EntityId][targetEnt.EntityId].lastComputationTime + AWARENESS_STALE_AFTER_TIME_MS;
		}

		public static bool EntityIsAwareOfTargetEntity(Entity ent, Entity targetEnt) {
			return awarenessData[ent.EntityId][targetEnt.EntityId].isAware;
		}

		public static void UpdateOrCreateEntityAwarenessEntryForTargetEntity(Entity ent, Entity targetEnt, bool isAware) {
			if (!EntityHasAwarenessEntry(ent)) {
				awarenessData.Add(ent.EntityId, new Dictionary<long, AwarenessData>());
				awarenessData[ent.EntityId].Add(targetEnt.EntityId, new AwarenessData(isAware, ent.World.ElapsedMilliseconds));
			} else if (!EntityHasAwarenessEntryForTargetEntity(ent, targetEnt)) {
				awarenessData[ent.EntityId].Add(targetEnt.EntityId, new AwarenessData(isAware, ent.World.ElapsedMilliseconds));
			} else {
				AwarenessData targetData = awarenessData[ent.EntityId][targetEnt.EntityId];
				targetData.isAware = isAware;
				targetData.lastComputationTime = ent.World.ElapsedMilliseconds;
				awarenessData[ent.EntityId][targetEnt.EntityId] = targetData;
			}
		}

		public static bool GetEntityAwarenessForTargetEntity(Entity ent, Entity targetEnt) {
			return awarenessData[ent.EntityId][targetEnt.EntityId].isAware;
		}

		public static void OnDespawn(Entity entity, EntityDespawnData despawnData) {
			CleanUpEntries(entity);
		}

		public static void OnDeath(Entity entity, DamageSource damageSource) {
			CleanUpEntries(entity);
		}

		private static void CleanUpEntries(Entity entToCleanup) {
			// Remove all target entries.
			foreach (long entID in awarenessData.Keys) {
				if (awarenessData[entID].ContainsKey(entToCleanup.EntityId)) {
					awarenessData[entID].Remove(entToCleanup.EntityId);
				}
			}
			if (awarenessData.ContainsKey(entToCleanup.EntityId)) {
				awarenessData.Remove(entToCleanup.EntityId);
			}
		}

		public static bool IsAwareOfTarget(Entity searchingEntity, Entity targetEntity, float maxDist, float maxVerDist) {
			// Bulk ignore entities that we just don't care about, like butterflies.
			if (AiUtility.EntityCodeInArray(targetEntity, alwaysIgnoreEntityCodes)) {
				return false;
			}
			// We cannot percieve ourself as a target.
			if (searchingEntity == targetEntity) {
				return false;
			}
			// If no players are within a reasonable range, don't spot anything just return true to save overhead.
			if (!AiUtility.IsAnyPlayerWithinRangeOfPos(targetEntity.ServerPos.XYZ, 250, targetEntity.World)) {
				return false;
			}
			// Because traces and light checks are expensive, see if we have already run this calculation this frame and
			// if we have, use the saved value for the entity. This will prevent redundant calls to get the same data.
			if (AwarenessManager.EntityHasAwarenessEntry(searchingEntity)) {
				if (AwarenessManager.EntityHasAwarenessEntryForTargetEntity(searchingEntity, targetEntity)) {
					if (!AwarenessManager.EntityAwarenessEntryForTargetEntityIsStale(searchingEntity, targetEntity)) {
						return AwarenessManager.GetEntityAwarenessForTargetEntity(searchingEntity, targetEntity);
					}
				}
			}

			/// DISTANCE CHECK ///
			Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
			Vec3d selectionBoxMidPoint = searchingEntity.ServerPos.XYZ.Add(0.0, searchingEntity.SelectionBox.Y2 / 2f, 0.0).Ahead(searchingEntity.SelectionBox.XSize / 2f, 0f, searchingEntity.ServerPos.Yaw);
			double shortestDist = cuboidd.ShortestDistanceFrom(selectionBoxMidPoint);
			double shortestVertDist = Math.Abs(cuboidd.ShortestVerticalDistanceFrom(selectionBoxMidPoint.Y));

			// Scale Ai Awareness Based on How the player is Moving;
			double aiAwarenessVisionScalar = 1.0;

			if (targetEntity is EntityPlayer) {
				aiAwarenessVisionScalar = GetAiVisionAwarenessScalarForPlayerMovementType((EntityPlayer)targetEntity);
			}

			if (shortestDist >= (double)maxDist || shortestVertDist >= (double)maxVerDist) {
				AwarenessManager.UpdateOrCreateEntityAwarenessEntryForTargetEntity(searchingEntity, targetEntity, false);
				return false;
			}

			AwarenessManager.UpdateOrCreateEntityAwarenessEntryForTargetEntity(searchingEntity, targetEntity, false);
			return false;
		}

		public static double GetAiVisionAwarenessScalarForPlayerMovementType(EntityPlayer playerEnt) {
			if (playerEnt.Controls.Sneak && playerEnt.OnGround) {
				return AI_VISION_AWARENESS_SNEAK_MODIFIER;
			} else if (playerEnt.Controls.Sprint && playerEnt.OnGround) {
				return AI_VISION_AWARENESS_SPRINT_MODIFIER;
			} else if (playerEnt.Controls.TriesToMove && playerEnt.OnGround) {
				return AI_VISION_AWARENESS_WALK_MODIFIER;
			}
			return AI_VISION_AWARENESS_STANDNG_MODIFIER;
		}

		public static bool EntityHasNightVison(Entity entity) {
			if (entity.Properties.Attributes.KeyExists("hasNightVision")) {
				return entity.Properties.Attributes["hasNightVision"].AsBool();
			}
			return false;
		}
	}
}