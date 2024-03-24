using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace SoldierCommand {
	public class HumanAnimationManager : AnimationManager {
		public HashSet<string> PersonalizedAnimations = new HashSet<string>(new string[] { "idle1", "idle2", "idle3", "walk", "sprint", "sneakidle", "sneak", "swimidle", "swim", "ladderidle", "ladderup", "ladderdown", "hit", "spearidle", "spearready", "spearhit", "falx", "swordhit", "swordhit2", "knifestab", "cleaverhit", "crudeOarIdle", "crudeOarStandingReady", "crudeOarHit", "gunidle", "gunready", "gunaim", "gunhit", "woundedidle", "hurtpose", "hurt", "cheer", "wave", "cry", "laugh", "rage", "facepalm", "bow", "nod", "headscratch", "cough", "stretch", "yawn", "lookaround", "drink", "coldidle", "protecteyes" });

		protected string lastActiveHeldReadyAnimation;
		protected string lastActiveRightHeldIdleAnimation;
		protected string lastActiveLeftHeldIdleAnimation;

		protected string lastActiveHeldHitAnimation;
		protected string lastActiveHeldUseAnimation;

		public string lastRunningHeldHitAnimation;
		public string lastRunningHeldUseAnimation;

		EntityArcher soldierEntity;

		public override void Init(ICoreAPI api, Entity entity) {
			base.Init(api, entity);
			soldierEntity = entity as EntityArcher;
		}

		public override bool StartAnimation(string configCode) {
			if (PersonalizedAnimations.Contains(configCode.ToLowerInvariant())) {
				return StartAnimation(new AnimationMetaData() {
					Animation = configCode,
					Code = configCode,
					BlendMode = EnumAnimationBlendMode.Average,
					EaseOutSpeed = 10000,
					EaseInSpeed = 10000
				}.Init());
			}
			return base.StartAnimation(configCode);
		}

		public override bool StartAnimation(AnimationMetaData animdata) {
			if ((animdata.Code == "idle1" || animdata.Code == "idle2" || animdata.Code == "idle3" || animdata.Code == "laugh" || animdata.Code == "rage") && ActiveAnimationsByAnimCode.ContainsKey("wave")) {
				return false;
			}
			if (PersonalizedAnimations.Contains(animdata.Animation.ToLowerInvariant())) {
				animdata = animdata.Clone();
				animdata.Code = animdata.Animation;
				animdata.CodeCrc32 = AnimationMetaData.GetCrc32(animdata.Code);
			}
			return base.StartAnimation(animdata);
		}

		public override void StopAnimation(string code) {
			base.StopAnimation(code);
		}

		public override void OnAnimationStopped(string code) {
			base.OnAnimationStopped(code);
			if (entity.Alive && ActiveAnimationsByAnimCode.Count == 0) {
				StartAnimation(new AnimationMetaData() { Code = "idle1", Animation = "idle1", EaseOutSpeed = 10000, EaseInSpeed = 10000 });
			}
		}

		public class HumanPersonality {
			public float ChorldDelayMul = 1;
			public float PitchModifier = 1;
			public float VolumneModifier = 1;

			public HumanPersonality(float chordDelayMul, float pitchModifier, float volumneModifier) {
				ChorldDelayMul = chordDelayMul;
				PitchModifier = pitchModifier;
				VolumneModifier = volumneModifier;
			}
		}
	}
}