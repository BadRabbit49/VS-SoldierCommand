using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace SoldierCommand {
	public class SoldierUtility {
		public static bool CanFollowThis(Entity entity, Entity target) {
			// Target must be either a EntityArcher or EntityPlayer.
			if (target is EntityArcher || target is EntityPlayer) {
				BehaviorGearItems behaviorGearItems = entity.GetBehavior<BehaviorGearItems>();
				// Get the target's allegiances to a group or owner. If they have none just don't. 
				if (target is EntityArcher) {
					BehaviorGearItems targetGearItems = entity.GetBehavior<BehaviorGearItems>();
					if (targetGearItems.ownerUID == behaviorGearItems.ownerUID || targetGearItems.groupUID == behaviorGearItems.groupUID) {
						return true;
					}
				}
				// Get the target's group affiliation if they're a player.
				if (target is EntityPlayer player) {
					// Never attack the owner.
					if (player.PlayerUID == behaviorGearItems.ownerUID || player.Player.GetGroup(behaviorGearItems.groupUID) != null) {
						return true;
					}
				}
			}
			return false;
		}

		public static bool CanTargetThis(Entity entity, Entity target) {
			// Return nothing if the target is null.
			if (target  == null) {
				return false;
			}
			// Special check if the target is a soldier or player.
			if (target is EntityArcher || target is EntityPlayer) {
				// If PvP is disabled, don't even worry about continuing.
				if (entity.Api.World.Config.GetAsBool("PvpOff")) {
					return false;
				} else {
					// Assuming the entity is an EntityArcher or atleast has the ItemGear behavior.
					BehaviorGearItems behaviorGearItems = entity.GetBehavior<BehaviorGearItems>();
					string owner = behaviorGearItems.ownerUID;
					int group = behaviorGearItems.groupUID;
					// Get the target's allegiances to a group or owner. If they have none just don't. 
					if (target is EntityArcher) {
						BehaviorGearItems targetGearItems = entity.GetBehavior<BehaviorGearItems>();
						string targetOwner = targetGearItems.ownerUID;
						int targetGroup = targetGearItems.groupUID;
						if (targetOwner == owner || targetGroup == group) {
							return false;
						}
					}
					// Get the target's group affiliation if they're a player.
					if (target is EntityPlayer player) {
						// Never attack the owner.
						if (player.PlayerUID == owner) {
							return false;
						}
						// A bit archaic, but if they're in any groups beside our own, attack them on sight.
						if (player.Player.GetGroup(group) != null) {
							return player.Player.Groups.Length != 0;
						} else {
							return false;
						}
					}
				}
			}
			return target.Alive;
		}

		public static bool ShouldFleeNow(Entity entity, Entity target) {
			if (target.Alive && target.HasBehavior<EntityBehaviorHealth>()) {
				// Only decide to run if PvP is enabled and the entity is a player, or if the entity is another soldier and not part of the same group.
				if (entity.HasBehavior<BehaviorGearItems>() && (target.HasBehavior<BehaviorGearItems>() || target is EntityPlayer)) {
					// Get faction group and or owner UIDs for comparison. Check if we can dismiss on the same side?
					if (entity.GetBehavior<BehaviorGearItems>().groupUID == target.GetBehavior<BehaviorGearItems>().groupUID || entity.GetBehavior<BehaviorGearItems>().ownerUID == target.GetBehavior<BehaviorGearItems>().ownerUID) {
						return false;
					}
					if (target is EntityPlayer player && player.Player.GetGroup(entity.GetBehavior<BehaviorGearItems>().groupUID) != null) {
						return false;
					}
					if (target is EntityPlayer && entity.Api.World.Config.GetAsBool("PvpOff")) {
						return false;
					}
				}
				// Get health pools, armor, and weapons, then compare liklihood of victory.
				float entityCurHealth = entity.GetBehavior<EntityBehaviorHealth>().Health;
				float entityMaxHealth = entity.GetBehavior<EntityBehaviorHealth>().MaxHealth;
				float targetCurHealth = target.GetBehavior<EntityBehaviorHealth>().Health;
				float targetMaxHealth = target.GetBehavior<EntityBehaviorHealth>().MaxHealth;
				Vec3d targetPosOffset = new Vec3d().Set(entity.World.Rand.NextDouble() * 2.0 - 1.0, 0.0, entity.World.Rand.NextDouble() * 2.0 - 1.0);
				double targetX = target.ServerPos.X + targetPosOffset.X;
				double targetY = target.ServerPos.Y;
				double targetZ = target.ServerPos.Z + targetPosOffset.Z;
				// Now if we have some breathing room to reevaluate the situation, see if we should continue this fight or not.
				if (entity.ServerPos.SquareDistanceTo(targetX, targetY, targetZ) > 3) {
					// Determine if enemy has more health and is stronger.
					return (entityCurHealth / entityMaxHealth) < 0.25 && (targetCurHealth / targetMaxHealth) > 0.25;
				}
			}
			return false;
		}
	}
}