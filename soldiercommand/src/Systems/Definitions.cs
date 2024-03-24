﻿using System.Collections.Generic;
using Vintagestory.API.Common;

namespace SoldierCommand {
	public static class WeaponDefinitions {
		// Ranged Weapons.
		public static List<AssetLocation> AcceptedRange = new List<AssetLocation>() {
			// Vintage Story
			new AssetLocation("game:bow-crude"),
			new AssetLocation("game:bow-simple"),
			new AssetLocation("game:bow-long"),
			new AssetLocation("game:bow-recurve"),
			new AssetLocation("game:sling"),
			// Maltiez Firearms & Crossbows
			new AssetLocation("maltiezfirearms:pistol"),
			new AssetLocation("maltiezfirearms:arquebus"),
			new AssetLocation("maltiezfirearms:musket"),
			new AssetLocation("maltiezfirearms:carbine"),
			new AssetLocation("maltiezcrossbows:crossbow-simple"),
			new AssetLocation("maltiezcrossbows:crossbow-stirrup"),
			new AssetLocation("maltiezcrossbows:crossbow-goatsfoot"),
			new AssetLocation("maltiezcrossbows:crossbow-windlass")
		};
		// Ammunitions.
		public static List<AssetLocation> AcceptedAmmos = new List<AssetLocation>() {
			// Vintage Story
			new AssetLocation("game:arrow-crude"),
			new AssetLocation("game:arrow-flint"),
			new AssetLocation("game:arrow-copper"),
			new AssetLocation("game:arrow-tinbronze"),
			new AssetLocation("game:arrow-gold"),
			new AssetLocation("game:arrow-silver"),
			new AssetLocation("game:arrow-bismuthbronze"),
			new AssetLocation("game:arrow-blackbronze"),
			new AssetLocation("game:arrow-iron"),
			new AssetLocation("game:arrow-meteoriciron"),
			new AssetLocation("game:arrow-steel"),
			new AssetLocation("game:spear-chert"),
			new AssetLocation("game:spear-granite"),
			new AssetLocation("game:spear-andesite"),
			new AssetLocation("game:spear-peridotite"),
			new AssetLocation("game:spear-basalt"),
			new AssetLocation("game:spear-flint"),
			new AssetLocation("game:spear-obsidian"),
			new AssetLocation("game:spear-scrap"),
			new AssetLocation("game:spear-copper"),
			new AssetLocation("game:spear-bismuthbronze"),
			new AssetLocation("game:spear-tinbronze"),
			new AssetLocation("game:spear-blackbronze"),
			new AssetLocation("game:spear-ruined"),
			new AssetLocation("game:spear-hacking"),
			new AssetLocation("game:spear-ornategold"),
			new AssetLocation("game:spear-ornatesilver"),
			new AssetLocation("game:bullets-lead"),
			new AssetLocation("game:stone"),
			new AssetLocation("game:beenade"),
			// Maltiez Firearms & Crossbows
			new AssetLocation("maltiezfirearms:bullet-lead"),
			new AssetLocation("maltiezfirearms:bullet-copper"),
			new AssetLocation("maltiezfirearms:bullet-steel"),
			new AssetLocation("maltiezfirearms:slug-lead"),
			new AssetLocation("maltiezfirearms:slug-copper"),
			new AssetLocation("maltiezfirearms:slug-steel"),
			new AssetLocation("maltiezcrossbows:bolt-crude"),
			new AssetLocation("maltiezcrossbows:bolt-copper"),
			new AssetLocation("maltiezcrossbows:bolt-tinbronze"),
			new AssetLocation("maltiezcrossbows:bolt-bismuthbronze"),
			new AssetLocation("maltiezcrossbows:bolt-blackbronze"),
			new AssetLocation("maltiezcrossbows:bolt-iron"),
			new AssetLocation("maltiezcrossbows:bolt-meteoriciron"),
			new AssetLocation("maltiezcrossbows:bolt-steel"),
		};
		// Get base damage values of these weapons without their ammo.
		public static Dictionary<AssetLocation, float> WeaponDamage = new Dictionary<AssetLocation, float> {
			// Vintage Story
			{ new AssetLocation("game:bow-crude"), 3f },
			{ new AssetLocation("game:bow-simple"), 3.25f },
			{ new AssetLocation("game:bow-long"), 3.75f },
			{ new AssetLocation("game:bow-recurve"), 4f },
			{ new AssetLocation("game:sling"), 2.5f },
			// Maltiez Firearms & Crossbows
			{ new AssetLocation("maltiezfirearms:pistol"), 0f },
			{ new AssetLocation("maltiezfirearms:arquebus"), 0f },
			{ new AssetLocation("maltiezfirearms:musket"), 0f },
			{ new AssetLocation("maltiezfirearms:carbine"), 0f },
			{ new AssetLocation("maltiezcrossbows:crossbow-simple"), 1f },
			{ new AssetLocation("maltiezcrossbows:crossbow-stirrup"),  1.5f },
			{ new AssetLocation("maltiezcrossbows:crossbow-goatsfoot"), 2.2f },
			{ new AssetLocation("maltiezcrossbows:crossbow-windlass"), 4f },
		};
		// Projectile damage values that are added onto the base weapon.
		public static Dictionary<AssetLocation, float> BulletDamage = new Dictionary<AssetLocation, float> {
			// Vintage Story
			{ new AssetLocation("game:arrow-crude"), -0.75f },
			{ new AssetLocation("game:arrow-flint"), -0.50f },
			{ new AssetLocation("game:arrow-copper"), 0f },
			{ new AssetLocation("game:arrow-tinbronze"), 1f },
			{ new AssetLocation("game:arrow-gold"), 1f },
			{ new AssetLocation("game:arrow-silver"), 1f },
			{ new AssetLocation("game:arrow-bismuthbronze"), 1f },
			{ new AssetLocation("game:arrow-blackbronze"), 1.5f },
			{ new AssetLocation("game:arrow-iron"), 2f },
			{ new AssetLocation("game:arrow-meteoriciron"), 2.25f },
			{ new AssetLocation("game:arrow-steel"), 2.5f },
			{ new AssetLocation("game:spear-chert"), 4f },
			{ new AssetLocation("game:spear-granite"), 4f },
			{ new AssetLocation("game:spear-andesite"), 4f },
			{ new AssetLocation("game:spear-peridotite"), 4f },
			{ new AssetLocation("game:spear-basalt"), 4f },
			{ new AssetLocation("game:spear-flint"), 5f },
			{ new AssetLocation("game:spear-obsidian"), 5.25f },
			{ new AssetLocation("game:spear-scrap"), 5.75f },
			{ new AssetLocation("game:spear-copper"), 5.75f },
			{ new AssetLocation("game:spear-bismuthbronze"), 6.5f },
			{ new AssetLocation("game:spear-tinbronze"), 7.5f },
			{ new AssetLocation("game:spear-blackbronze"), 8f },
			{ new AssetLocation("game:spear-ruined"), 8f },
			{ new AssetLocation("game:spear-hacking"), 7f },
			{ new AssetLocation("game:spear-ornategold"), 8.25f },
			{ new AssetLocation("game:spear-ornatesilver"), 8.25f },
			// Maltiez Firearms & Crossbows
			{ new AssetLocation("maltiezfirearms:bullet-lead"), 16f },
			{ new AssetLocation("maltiezfirearms:bullet-copper"), 8f },
			{ new AssetLocation("maltiezfirearms:bullet-steel"), 10f },
			{ new AssetLocation("maltiezfirearms:slug-lead"), 38f },
			{ new AssetLocation("maltiezfirearms:slug-copper"), 20f },
			{ new AssetLocation("maltiezfirearms:slug-steel"), 25f },
			{ new AssetLocation("maltiezcrossbows:bolt-crude"), 5f },
			{ new AssetLocation("maltiezcrossbows:bolt-copper"), 6f },
			{ new AssetLocation("maltiezcrossbows:bolt-tinbronze"), 7f },
			{ new AssetLocation("maltiezcrossbows:bolt-bismuthbronze"), 7f },
			{ new AssetLocation("maltiezcrossbows:bolt-blackbronze"), 7f },
			{ new AssetLocation("maltiezcrossbows:bolt-iron"), 7f },
			{ new AssetLocation("maltiezcrossbows:bolt-meteoriciron"), 7f },
			{ new AssetLocation("maltiezcrossbows:bolt-steel"), 9f }
		};
		// Resurrectors used to revive or heal soldiers. USES CODES.
		public static Dictionary<AssetLocation, float> Resurrectors = new Dictionary<AssetLocation, float> {
			// Vintage Story
			{ new AssetLocation("game:bandage-clean"), 4f },
			{ new AssetLocation("game:bandage-alcoholed"), 8 },
			{ new AssetLocation("game:poultice-reed-horsetail"), 1 },
			{ new AssetLocation("game:poultice-reed-honey-sulfur"), 2 },
			{ new AssetLocation("game:poultice-linen-horsetail"), 2 },
			{ new AssetLocation("game:poultice-linen-honey-sulfur"), 3 }
		};
		// Audios to play while aiming.
		public static Dictionary<AssetLocation, AssetLocation> wepnAimAudio = new Dictionary<AssetLocation, AssetLocation> {
			// Vintage Story
			{ new AssetLocation("game:bow-crude"), new AssetLocation("game:sounds/bow-draw") },
			{ new AssetLocation("game:bow-simple"), new AssetLocation("game:sounds/bow-draw") },
			{ new AssetLocation("game:bow-long"), new AssetLocation("game:sounds/bow-draw") },
			{ new AssetLocation("game:bow-recurve"), new AssetLocation("game:sounds/bow-draw") },
			{ new AssetLocation("game:sling"), new AssetLocation("game:sounds/bow-draw") },
			{ new AssetLocation("game:spear-chert"), null },
			{ new AssetLocation("game:spear-granite"), null },
			{ new AssetLocation("game:spear-andesite"), null },
			{ new AssetLocation("game:spear-peridotite"), null },
			{ new AssetLocation("game:spear-basalt"), null },
			{ new AssetLocation("game:spear-flint"), null },
			{ new AssetLocation("game:spear-obsidian"), null },
			{ new AssetLocation("game:spear-scrap"), null },
			{ new AssetLocation("game:spear-copper"), null },
			{ new AssetLocation("game:spear-bismuthbronze"), null },
			{ new AssetLocation("game:spear-tinbronze"), null },
			{ new AssetLocation("game:spear-blackbronze"), null },
			{ new AssetLocation("game:spear-ruined"), null },
			{ new AssetLocation("game:spear-hacking"), null },
			{ new AssetLocation("game:spear-ornategold"), null },
			{ new AssetLocation("game:spear-ornatesilver"), null },
			// Maltiez Firearms & Crossbows
			{ new AssetLocation("maltiezfirearms:pistol"), new AssetLocation("maltiezfirearms:sounds/pistol/flint-raise") },
			{ new AssetLocation("maltiezfirearms:arquebus"), new AssetLocation("maltiezfirearms:sounds/arquebus/powder-prime") },
			{ new AssetLocation("maltiezfirearms:musket"), new AssetLocation("maltiezfirearms:sounds/musket/musket-cock") },
			{ new AssetLocation("maltiezfirearms:carbine"), new AssetLocation("maltiezfirearms:sounds/musket/musket-cock") },
			{ new AssetLocation("maltiezcrossbows:crossbow-simple"), new AssetLocation("maltiezcrossbows:sounds/loading/wooden-click") },
			{ new AssetLocation("maltiezcrossbows:crossbow-stirrup"), new AssetLocation("maltiezcrossbows:sounds/loading/wooden-click") },
			{ new AssetLocation("maltiezcrossbows:crossbow-goatsfoot"), new AssetLocation("maltiezcrossbows:sounds/loading/metal-click") },
			{ new AssetLocation("maltiezcrossbows:crossbow-windlass"), new AssetLocation("maltiezcrossbows:sounds/loading/metal-click") }
		};
		// Audios to play while firing.
		public static Dictionary<AssetLocation, List<AssetLocation>> wepnHitAudio = new Dictionary<AssetLocation, List<AssetLocation>> {
			// Vintage Story
			{ new AssetLocation("game:bow-crude"), new List<AssetLocation> { new AssetLocation("game:sounds/bow-release") } },
			{ new AssetLocation("game:bow-simple"), new List<AssetLocation> { new AssetLocation("game:sounds/bow-release") } },
			{ new AssetLocation("game:bow-long"), new List<AssetLocation> { new AssetLocation("game:sounds/bow-release") } },
			{ new AssetLocation("game:bow-recurve"), new List<AssetLocation> { new AssetLocation("game:sounds/bow-release") } },
			{ new AssetLocation("game:sling"), new List<AssetLocation> { new AssetLocation("game:sounds/tool/sling1") } },
			{ new AssetLocation("game:spear"), new List<AssetLocation> { new AssetLocation("game:sounds/bow-release") } },
			// Maltiez Firearms & Crossbows
			{ new AssetLocation("maltiezfirearms:pistol"), new List<AssetLocation> { new AssetLocation("maltiezfirearms:sounds/pistol/pistol-fire-1"), new AssetLocation("maltiezfirearms:sounds/pistol/pistol-fire-2"), new AssetLocation("maltiezfirearms:sounds/pistol/pistol-fire-3"), new AssetLocation("maltiezfirearms:sounds/pistol/pistol-fire-4"), } },
			{ new AssetLocation("maltiezfirearms:arquebus"), new List<AssetLocation> { new AssetLocation("maltiezfirearms:sounds/arquebus/arquebus-fire-1"), new AssetLocation("maltiezfirearms:sounds/arquebus/arquebus-fire-2"), new AssetLocation("maltiezfirearms:sounds/arquebus/arquebus-fire-3") } },
			{ new AssetLocation("maltiezfirearms:musket"), new List<AssetLocation> { new AssetLocation("maltiezfirearms:sounds/musket/musket-fire-1"), new AssetLocation("maltiezfirearms:sounds/musket/musket-fire-2"), new AssetLocation("maltiezfirearms:sounds/musket/musket-fire-3"), new AssetLocation("maltiezfirearms:sounds/musket/musket-fire-4") } },
			{ new AssetLocation("maltiezfirearms:carbine"), new List<AssetLocation> { new AssetLocation("maltiezfirearms:sounds/musket/musket-fire-1"), new AssetLocation("maltiezfirearms:sounds/musket/musket-fire-2"), new AssetLocation("maltiezfirearms:sounds/musket/musket-fire-3"), new AssetLocation("maltiezfirearms:sounds/musket/musket-fire-4") } },
			{ new AssetLocation("maltiezcrossbows:crossbow-simple"), new List<AssetLocation> { new AssetLocation( "maltiezcrossbows:sounds/release/simple-0"), new AssetLocation("maltiezcrossbows:sounds/release/simple-1"), new AssetLocation("maltiezcrossbows:sounds/release/simple-2"), new AssetLocation("maltiezcrossbows:sounds/release/simple-3") } },
			{ new AssetLocation("maltiezcrossbows:crossbow-stirrup"), new List<AssetLocation> { new AssetLocation("maltiezcrossbows:sounds/release/stirrup-0"), new AssetLocation("maltiezcrossbows:sounds/release/stirrup-1"), new AssetLocation("maltiezcrossbows:sounds/release/stirrup-2"), new AssetLocation("maltiezcrossbows:sounds/release/stirrup-3"), new AssetLocation("maltiezcrossbows:sounds/release/stirrup-4"), new AssetLocation("maltiezcrossbows:sounds/release/stirrup-5"), new AssetLocation("maltiezcrossbows:sounds/release/stirrup-6") } },
			{ new AssetLocation("maltiezcrossbows:crossbow-goatsfoot"), new List<AssetLocation> { new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-0"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-1"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-2"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-3"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-4"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-5"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-6"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-7"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-8"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-9"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-10"), new AssetLocation("maltiezcrossbows:sounds/release/goatsfoot-11") } },
			{ new AssetLocation("maltiezcrossbows:crossbow-windlass"), new List<AssetLocation> { new AssetLocation("maltiezcrossbows:sounds/release/windlass-0"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-1"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-2"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-3"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-4"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-5"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-6"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-7"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-8"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-9"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-10"), new AssetLocation("maltiezcrossbows:sounds/release/windlass-11") } }
		};
		// Animation to use while aiming.
		public static Dictionary<AssetLocation, int> weaponsAnims = new Dictionary<AssetLocation, int> {
			// Vintage Story
			{ new AssetLocation("game:bow-crude"), 0 },
			{ new AssetLocation("game:bow-simple"), 1 },
			{ new AssetLocation("game:bow-long"), 2 },
			{ new AssetLocation("game:bow-recurve"), 3 },
			{ new AssetLocation("game:sling"), 4 },
			{ new AssetLocation("game:spear-chert"), 6 },
			{ new AssetLocation("game:spear-granite"), 6 },
			{ new AssetLocation("game:spear-andesite"), 6 },
			{ new AssetLocation("game:spear-peridotite"), 6 },
			{ new AssetLocation("game:spear-basalt"), 6 },
			{ new AssetLocation("game:spear-flint"), 6 },
			{ new AssetLocation("game:spear-obsidian"), 6 },
			{ new AssetLocation("game:spear-scrap"), 6 },
			{ new AssetLocation("game:spear-copper"), 6 },
			{ new AssetLocation("game:spear-bismuthbronze"), 6 },
			{ new AssetLocation("game:spear-tinbronze"), 6 },
			{ new AssetLocation("game:spear-blackbronze"), 6 },
			{ new AssetLocation("game:spear-ruined"), 6 },
			{ new AssetLocation("game:spear-hacking"), 6 },
			{ new AssetLocation("game:spear-ornategold"), 6 },
			{ new AssetLocation("game:spear-ornatesilver"), 6 },
			// Maltiez Firearms & Crossbows
			{ new AssetLocation("maltiezfirearms:pistol"), 7 },
			{ new AssetLocation("maltiezfirearms:arquebus"), 7 },
			{ new AssetLocation("maltiezfirearms:musket"), 7 },
			{ new AssetLocation("maltiezfirearms:carbine"), 7 },
			{ new AssetLocation("maltiezcrossbows:crossbow-simple"), 7 },
			{ new AssetLocation("maltiezcrossbows:crossbow-stirrup"), 7 },
			{ new AssetLocation("maltiezcrossbows:crossbow-goatsfoot"), 7 },
			{ new AssetLocation("maltiezcrossbows:crossbow-windlass"), 7 }
		};
	}
	public static class AnimationDefinitions {
		public static List<WeaponAnims> WeaponAnimations { get; set; } = new List<WeaponAnims>(new WeaponAnims[] {
			// Defaults
			new WeaponAnims() {
				itemCode = null,
				idleAnim = "Idle1",
				idleTime = 1f,
				walkAnim = "Walk",
				walkTime = 1.3f,
				moveAnim = "Sprint",
				moveTime = 0.6f,
				drawAnim = null,
				drawTime = 1f,
				fireAnim = null,
				fireTime = 1f,
				loadAnim = null,
				loadTime = 1f,
				bashAnim = null,
				bashTime = 1f,
				stabAnim = null,
				stabTime = 1f,
				hit1Anim = null,
				hit1Time = 1f,
				hit2Anim = null,
				hit2Time = 1f
			},
			// Vintage Story
			new WeaponAnims() {
				itemCode = new AssetLocation("game:bow-crude"),
				idleAnim = "BowIdleCrude",
				idleTime = 1f,
				walkAnim = "BowWalkCrude",
				walkTime = 1.3f,
				moveAnim = "BowMoveCrude",
				moveTime = 0.6f,
				drawAnim = "BowDrawCrude",
				drawTime = 1f,
				fireAnim = "BowFireCrude",
				fireTime = 1f,
				loadAnim = "BowLoadCrude",
				loadTime = 1f,
				bashAnim = "BowBashCrude",
				bashTime = 1f,
				stabAnim = null,
				stabTime = 1f,
				hit1Anim = null,
				hit1Time = 1f,
				hit2Anim = null,
				hit2Time = 1f
			},
			// Maltiez Firearms
			new WeaponAnims() {
				itemCode = new AssetLocation("maltiezfirearms:pistol"),
				idleAnim = "GunIdlePistol",
				idleTime = 1f,
				walkAnim = "GunWalkPistol",
				walkTime = 1.3f,
				moveAnim = "GunMovePistol",
				moveTime = 0.6f,
				drawAnim = "GunDrawPistol",
				drawTime = 1f,
				fireAnim = "GunFireArquebus",
				fireTime = 1f,
				loadAnim = "Hit",
				loadTime = 1f,
				bashAnim = "Hit",
				bashTime = 1f,
				stabAnim = null,
				stabTime = 1f,
				hit1Anim = null,
				hit1Time = 1f,
				hit2Anim = null,
				hit2Time = 1f
			},
			new WeaponAnims() {
				itemCode = new AssetLocation("maltiezfirearms:arquebus"),
				idleAnim = "GunIdleArquebus",
				idleTime = 1f,
				walkAnim = "GunWalkArquebus",
				walkTime = 1.3f,
				moveAnim = "GunMoveArquebus",
				moveTime = 0.6f,
				drawAnim = "GunDrawArquebus",
				drawTime = 1f,
				fireAnim = "GunFireArquebus",
				fireTime = 1f,
				loadAnim = "GunLoadArquebus",
				loadTime = 1f,
				bashAnim = "GunBashArquebus",
				bashTime = 1f,
				stabAnim = null,
				stabTime = 1f,
				hit1Anim = null,
				hit1Time = 1f,
				hit2Anim = null,
				hit2Time = 1f
			},
			new WeaponAnims() {
				itemCode = new AssetLocation("maltiezfirearms:musket"),
				idleAnim = "GunIdleMusket",
				idleTime = 1f,
				walkAnim = "GunWalkMusket",
				walkTime = 1.3f,
				moveAnim = "GunMoveMusket",
				moveTime = 0.6f,
				drawAnim = "GunDrawMusket",
				drawTime = 1f,
				fireAnim = "GunFireMusket",
				fireTime = 1f,
				loadAnim = "GunLoadMusket",
				loadTime = 1f,
				bashAnim = "GunBashMusket",
				bashTime = 1f,
				stabAnim = "GunStabMusket",
				stabTime = 1f,
				hit1Anim = null,
				hit1Time = 1f,
				hit2Anim = null,
				hit2Time = 1f
			},
			new WeaponAnims() {
				itemCode = new AssetLocation("maltiezfirearms:carbine"),
				idleAnim = "GunIdleArquebus",
				idleTime = 1f,
				walkAnim = "GunWalkArquebus",
				walkTime = 1.3f,
				moveAnim = "GunMoveArquebus",
				moveTime = 0.6f,
				drawAnim = "GunDrawArquebus",
				drawTime = 1f,
				fireAnim = "GunFireArquebus",
				fireTime = 1f,
				loadAnim = "GunLoadArquebus",
				loadTime = 1f,
				bashAnim = "GunBashArquebus",
				bashTime = 1f,
				stabAnim = null,
				stabTime = 1f,
				hit1Anim = null,
				hit1Time = 1f,
				hit2Anim = null,
				hit2Time = 1f
			}
		});
	}
}