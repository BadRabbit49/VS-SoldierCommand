using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;

namespace SoldierCommand {
	public class AiTaskSoldierMeleeAttack : AiTaskMeleeAttack {
		public AnimationMetaData baseAnimMeta { get; set; }
        public AnimationMetaData stabAnimMeta { get; set; }
        public AnimationMetaData slashAnimMeta { get; set; }
        public float unarmedDamage { get; set; }
        public float armedDamageMultiplier { get; set; }
		public AiTaskSoldierMeleeAttack(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			baseAnimMeta = animMeta;
			unarmedDamage = damage;
			armedDamageMultiplier = taskConfig["armedDamageMultiplier"].AsFloat(4);
			if (taskConfig["stabanimation"].Exists) {
				stabAnimMeta = new AnimationMetaData() {
					Code = taskConfig["stabanimation"].AsString()?.ToLowerInvariant(),
					Animation = taskConfig["stabanimation"].AsString()?.ToLowerInvariant(),
					AnimationSpeed = taskConfig["stabanimationSpeed"].AsFloat(1f)
				}.Init();
			}
			if (taskConfig["slashanimation"].Exists) {
				slashAnimMeta = new AnimationMetaData() {
					Code = taskConfig["slashanimation"].AsString()?.ToLowerInvariant(),
					Animation = taskConfig["slashanimation"].AsString()?.ToLowerInvariant(),
					AnimationSpeed = taskConfig["slashanimationSpeed"].AsFloat(1f)
				}.Init();
			}
		}

		public override bool IsTargetableEntity(Entity ent, float range, bool ignoreEntityCode = false) {
			string ownerUID = (entity as EntityArcher).ownerUID;
			if (ent is EntityPlayer player) {
				if (ownerUID != null && player.PlayerUID == ownerUID) {
					return false;
				}
				if (SoldierConfig.Current.PvpOff && player.PlayerUID != ownerUID) {
					return false;
				}
			}
			if (attackedByEntity == ent) {
				return base.IsTargetableEntity(ent, range, true);
			}
			return base.IsTargetableEntity(ent, range, ignoreEntityCode);
		}

		public override void StartExecute() {
			EntityArcher thisEnt = (entity as EntityArcher);
			if (thisEnt.RightHandItemSlot != null && !thisEnt.RightHandItemSlot.Empty) {
				damage = Math.Max(thisEnt.RightHandItemSlot.Itemstack.Item.AttackPower * armedDamageMultiplier, unarmedDamage);
				if (thisEnt.RightHandItemSlot.Itemstack.Item.Code.Path.Contains("spear")) {
					animMeta = stabAnimMeta;
				} else {
					animMeta = slashAnimMeta;
				}
			} else {
				damage = unarmedDamage;
				animMeta = baseAnimMeta;
			}
			base.StartExecute();
		}

		public void OnAllyAttacked(Entity byEntity) {
			if (targetEntity == null || !targetEntity.Alive) {
				targetEntity = byEntity;
			}
		}
	}
}