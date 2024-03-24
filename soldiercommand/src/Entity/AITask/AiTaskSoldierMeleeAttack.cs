using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;
using System.Linq;

namespace SoldierCommand {
	public class AiTaskSoldierMeleeAttack : AiTaskMeleeAttack {
		public AiTaskSoldierMeleeAttack(EntityAgent entity) : base(entity) { }
		public float unarmedDamage { get; set; } = 1;
		public AnimationMetaData SwordHit1AnimMeta { get; set; }
		public AnimationMetaData SwordHit2AnimMeta { get; set; }
		public AnimationMetaData SimpleHitAnimMeta { get; set; }
		public AnimationMetaData KnifeStabAnimMeta { get; set; }
		public AnimationMetaData SpearStabAnimMeta { get; set; }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			SwordHit1AnimMeta = new AnimationMetaData() {
				Animation = "SwordHit1".ToLowerInvariant(),
				Code = "SwordHit1".ToLowerInvariant(),
			}.Init();
			SwordHit2AnimMeta = new AnimationMetaData() {
				Animation = "SwordHit2".ToLowerInvariant(),
				Code = "SwordHit2".ToLowerInvariant(),
			}.Init();
			SimpleHitAnimMeta = new AnimationMetaData() {
				Animation = "FalxHit".ToLowerInvariant(),
				Code = "FalxHit".ToLowerInvariant(),
			}.Init();
			KnifeStabAnimMeta = new AnimationMetaData() {
				Animation = "KnifeStab".ToLowerInvariant(),
				Code = "KnifeStab".ToLowerInvariant(),
			}.Init();
			SpearStabAnimMeta = new AnimationMetaData() {
				Animation = "SpearHit".ToLowerInvariant(),
				Code = "SpearHit".ToLowerInvariant(),
			}.Init();
		}
		
		public override bool IsTargetableEntity(Entity ent, float range, bool ignoreEntityCode = false) {
			if (ent?.Alive == true) {
				if (attackedByEntity == ent) {
					return true;
				}
				if (SoldierUtility.CanTargetThis(entity, ent)) {
					return true;
				}
			}
			return base.IsTargetableEntity(ent, range, ignoreEntityCode);
		}

		public override void StartExecute() {
			// Initialize a random attack animation and sounds!
			Random rnd = new Random();
			if (entity.RightHandItemSlot != null && !entity.RightHandItemSlot.Empty) {
				damage = entity.RightHandItemSlot.Itemstack.Item.AttackPower;
				if (entity.RightHandItemSlot.Itemstack.Item.Code.Path.Contains("spear")) {
					animMeta = SpearStabAnimMeta;
					entity.World.PlaySoundAt(new AssetLocation("game:sounds/player/stab"), entity, null, false);
				} else {
					switch (rnd.Next(1, 4)) {
						case 1:
							animMeta = SwordHit1AnimMeta;
							break;
						case 2:
							animMeta = SwordHit2AnimMeta;
							break;
						case 3:
							animMeta = SimpleHitAnimMeta;
							break;
						case 4:
							animMeta = KnifeStabAnimMeta;
							break;
						default:
							animMeta = SimpleHitAnimMeta;
							break;
					}
					switch (rnd.Next(1, 2)) {
						case 1:
							entity.World.PlaySoundAt(new AssetLocation("game:sounds/player/strike1"), entity, null, false);
							break;
						case 2:
							entity.World.PlaySoundAt(new AssetLocation("game:sounds/player/strike2"), entity, null, false);
							break;
						default:
							entity.World.PlaySoundAt(new AssetLocation("game:sounds/player/strike2"), entity, null, false);
							break;
					}
				}
			} else {
				damage = unarmedDamage;
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