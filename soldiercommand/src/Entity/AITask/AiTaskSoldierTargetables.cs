using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SoldierCommand {
	public class AiTaskSoldierTargetables : AiTaskBaseTargetable {
		protected List<Entity> allyForces = new List<Entity>();

		public AiTaskSoldierTargetables(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
		}

		public override bool ShouldExecute() {
			// Never execute this function.
			return false;
		}

		public override bool IsTargetableEntity(Entity ent, float range, bool ignoreEntityCode = false) {
			// We can never target our friends.
			if (ent != null && ent is EntityAgent) {
				if (ent is EntityArcher) {
					// TODO: Add group relations so soldiers don't attack other groups on sight. KOS is on for now.
					return ((EntityArcher)ent).groupUID == ((EntityArcher)entity).groupUID;
				}
				if (ent is EntityPlayer){
					if (((EntityPlayer)ent).Player.GetGroups().Length > 0) {
						return ((EntityPlayer)ent).Player.GetGroups().ElementAt(0).GroupUid != ((EntityArcher)entity).groupUID;
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

		protected virtual void UpdateHerdCount(float range = 60) {
			// Try to get herd entities from saved master list.
			allyForces = AiUtility.GetMasterHerdList(entity as EntityArcher);
			if (allyForces.Count == 0) {
				// Get all herd members.
				allyForces = new List<Entity>();
				entity.World.GetNearestEntity(entity.ServerPos.XYZ, range, range, (ent) => {
					if (ent is EntityAgent) {
						EntityAgent agent = ent as EntityAgent;
						if (agent.Alive && agent.HerdId == entity.HerdId) {
							allyForces.Add(agent);
						}
					}
					return false;
				});
				// Set new master list.
				AiUtility.SetMasterHerdList(entity as EntityArcher, allyForces);
			}
		}

		public override void OnEntityHurt(DamageSource damageSource, float damage) {
			Entity attacker = damageSource.SourceEntity;
			// Avoid friendly fire, but don't go after one another for revenge if it happens!
			if (attacker is EntityProjectile && damageSource.CauseEntity != null) {
				attacker = damageSource.CauseEntity;
			}
			if (attacker is EntityArcher) {
				EntityArcher attackerAgent = attacker as EntityArcher;
				if (attackerAgent.groupUID != (entity as EntityArcher).groupUID) {
					attackedByEntity = attackerAgent;
					attackedByEntityMs = entity.World.ElapsedMilliseconds;
				}
			} else {
				attackedByEntity = attacker;
				attackedByEntityMs = entity.World.ElapsedMilliseconds;
			}
		}

		public virtual void ClearTargetHistory() {
			targetEntity = null;
			attackedByEntity = null;
			attackedByEntityMs = 0;
		}
	}
}