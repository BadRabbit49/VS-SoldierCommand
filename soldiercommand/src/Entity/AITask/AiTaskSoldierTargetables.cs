using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SoldierCommand {
	public class AiTaskSoldierTargetables : AiTaskBaseTargetable {
		public AiTaskSoldierTargetables(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) { }

		public override bool ShouldExecute() {
			// Never execute this function.
			return false;
		}

		public override bool IsTargetableEntity(Entity ent, float range, bool ignoreEntityCode = false) {
			// We can never target our friends.
			if (ent != null && ent is EntityAgent) {
				EntityArcher thisEnt = entity as EntityArcher;
				if (ent is EntityArcher) {
					// TODO: Add group relations so soldiers don't attack other groups on sight. KOS is on for now.
					return (ent.GetBehavior<BehaviorGearItems>().cachedGroup == entity.GetBehavior<BehaviorGearItems>().cachedGroup);
				}
				if (ent is EntityPlayer){
					if (((EntityPlayer)ent).Player.GetGroups().Length > 0) {
						return ((EntityPlayer)ent).Player.GetGroups().Contains(entity.GetBehavior<BehaviorGearItems>().cachedGroup);
					} else {
						return false;
					}
				}
			}
			// Don't target projectiles, even if they hit us.
			if (ent is EntityProjectile) {
				return false;
			}
			return base.IsTargetableEntity(ent, range, ignoreEntityCode);
		}

		public override void OnEntityHurt(DamageSource damageSource, float damage) {
			Entity attacker = damageSource.SourceEntity;
			// Avoid friendly fire, but don't go after one another for revenge if it happens!
			if (attacker is EntityProjectile && damageSource.CauseEntity != null) {
				attacker = damageSource.CauseEntity;
			}
			if (attacker is EntityArcher) {
				if (attacker.GetBehavior<BehaviorGearItems>().groupUID != entity.GetBehavior<BehaviorGearItems>().groupUID) {
					attackedByEntity = attacker;
					attackedByEntityMs = entity.World.ElapsedMilliseconds;
					return;
				}
			} else if (attacker is EntityPlayer player) {
				if (!player.Player.Groups.Contains(entity.GetBehavior<BehaviorGearItems>().cachedGroup)) {
					attackedByEntity = attacker;
					attackedByEntityMs = entity.World.ElapsedMilliseconds;
					return;
				}
			} else {
				return;
			}
		}

		public virtual void ClearTargetHistory() {
			targetEntity = null;
			attackedByEntity = null;
			attackedByEntityMs = 0;
		}
	}
}