using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using ProtoBuf;
using HarmonyLib;
using System.Collections.Generic;

namespace SoldierCommand {
    public class SoldierCommand : ModSystem {
		Harmony harmony = new Harmony("badrabbit49.soldiercommand");
		public static ICoreServerAPI serverAPI;
		public static ICoreClientAPI clientAPI;

		public override void Start(ICoreAPI api) {
			base.Start(api);
			// Blocks
			api.RegisterBlockClass("BlockPost", typeof(BlockPost));
			// Block Behaviors
			api.RegisterBlockEntityBehaviorClass("Resupply", typeof(BlockBehaviorResupply));
			// Block Entities
			api.RegisterBlockEntityClass("GuardPost", typeof(BlockEntityPost));
			// Items
			api.RegisterItemClass("ItemBanner", typeof(ItemBanner));
			api.RegisterItemClass("ItemPeople", typeof(ItemPeople));
			// Entities
			api.RegisterEntity("EntityArcher", typeof(EntityArcher));
			// Entity Behaviors
			api.RegisterEntityBehaviorClass("SoldierTraverser", typeof(BehaviorTraverser));
			api.RegisterEntityBehaviorClass("SoldierGearItems", typeof(BehaviorGearItems));
			// AITasks
			AiTaskRegistry.Register<AiTaskFollowEntityLeader>("FollowEntityLeader");
			AiTaskRegistry.Register<AiTaskSoldierReturningTo>("SoldierReturningTo");
			AiTaskRegistry.Register<AiTaskSoldierRespawnPost>("SoldierRespawnPost");
			AiTaskRegistry.Register<AiTaskSoldierFleesEntity>("SoldierFleesEntity");
			AiTaskRegistry.Register<AiTaskSoldierGuardingPos>("SoldierGuardingPos");
			AiTaskRegistry.Register<AiTaskSoldierHealingSelf>("SoldierHealingSelf");
			AiTaskRegistry.Register<AiTaskSoldierMeleeAttack>("SoldierMeleeAttack");
			AiTaskRegistry.Register<AiTaskSoldierRangeAttack>("SoldierRangeAttack");
			AiTaskRegistry.Register<AiTaskSoldierSeeksEntity>("SoldierSeeksEntity");
			AiTaskRegistry.Register<AiTaskSoldierTargetables>("SoldierTargetables");
			AiTaskRegistry.Register<AiTaskSoldierTravelToPos>("SoldierTravelToPos");
			AiTaskRegistry.Register<AiTaskSoldierWanderAbout>("SoldierWanderAbout");
			// Patch Everything
			if (!Harmony.HasAnyPatches("badrabbit49.soldiercommand")) {
				harmony.PatchAll();
			}
		}

		public override void StartClientSide(ICoreClientAPI capi) {
			base.StartClientSide(capi);
			clientAPI = capi;
			capi.Network.RegisterChannel("soldiernetwork").RegisterMessageType<SoldierCommandMsg>().RegisterMessageType<SoldierProfileMsg>().SetMessageHandler<SoldierProfileMsg>(OnSoldierProfileMsgClient);
		}

		public override void StartServerSide(ICoreServerAPI sapi) {
			base.StartServerSide(sapi);
			serverAPI = sapi;
			sapi.Network.RegisterChannel("soldiernetwork").RegisterMessageType<SoldierCommandMsg>().SetMessageHandler<SoldierCommandMsg>(OnSoldierCommandMsg).RegisterMessageType<SoldierProfileMsg>().SetMessageHandler<SoldierProfileMsg>(OnSoldierProfileMsgServer);
		}

		public override void Dispose() {
			base.Dispose();
			// Unload and Unpatch everything from the mod.
			harmony?.UnpatchAll(Mod.Info.ModID);
		}

		private void OnSoldierCommandMsg(IServerPlayer fromPlayer, SoldierCommandMsg networkMessage) {
			EntityPlayer player = serverAPI.World.PlayerByUid(networkMessage.playerUID)?.Entity;
			EntityArcher target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityArcher;
		}

		private void OnSoldierProfileMsgServer(IServerPlayer fromPlayer, SoldierProfileMsg networkMessage) {
			EntityArcher target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityArcher;
			target.GetBehavior<EntityBehaviorNameTag>()?.SetName(networkMessage.soldierName);
			if (target?.HasBehavior<BehaviorGearItems>() == true) {
				BehaviorGearItems gearItems = target.GetBehavior<BehaviorGearItems>();
				// Reset values. Return to civilian life.
				if (networkMessage.abandon) {
					gearItems.enlistedStatus = EnlistedStatus.CIVILIAN;
					gearItems.ownerUID = null;
					gearItems.groupUID = 0;
				}
			}
		}

		private void OnSoldierProfileMsgClient(SoldierProfileMsg networkMessage) {
			if (clientAPI != null) {
				new SoldierProfile(clientAPI, networkMessage.targetEntityUID).TryOpen();
			}
		}
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class SoldierCommandMsg {
		public string playerUID;
		public string commandName;
		public string commandType;
		public long targetEntityUID;
	}
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class SoldierProfileMsg {
		public string soldierName;
		public bool multiplyAllowed;
		public bool abandon;
		public long targetEntityUID;
		public long oldEntityUID;
	}
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class WeaponAnims {
		public AssetLocation itemCode;
		public string idleAnim;
		public float idleTime;
		public string walkAnim;
		public float walkTime;
		public string moveAnim;
		public float moveTime;
		public string drawAnim;
		public float drawTime;
		public string fireAnim;
		public float fireTime;
		public string loadAnim;
		public float loadTime;
		public string bashAnim;
		public float bashTime;
		public string stabAnim;
		public float stabTime;
		public string hit1Anim;
		public float hit1Time;
		public string hit2Anim;
		public float hit2Time;
	}
}