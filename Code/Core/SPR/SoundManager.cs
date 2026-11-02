using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using LapinLibrary;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR;

public class SoundManager : Manager
{
	public enum BGMType
	{
		A = 0,
		B = 1,
		Library = 2,
		None = 63
	}

	[Serializable]
	public struct SectorEventInfo
	{
		[SerializeField]
		public SectorName sectorName;

		[SerializeField]
		public EventReference eventReference;
	}

	[Serializable]
	public struct SpecialEventInfo
	{
		[SerializeField]
		public string key;

		[SerializeField]
		public EventReference eventReference;
	}

	[Serializable]
	public struct Sound3DAttributeInfo
	{
		[SerializeField]
		public string key;

		[SerializeField]
		public List<Sound3DPositionInfo> fieldSourceDict;
	}

	[Serializable]
	public struct Sound3DPositionInfo
	{
		[SerializeField]
		public FieldID fieldId;

		[SerializeField]
		public Vector2 sourcePosition;
	}

	public enum MaterialType
	{
		Rock,
		Wood,
		Plant,
		Metal,
		RockHeavy
	}

	public enum CreatureMaterialType
	{
		Rock,
		Plant,
		Creature,
		Mimic,
		SeedKeeper
	}

	public enum GroundMaterialType
	{
		Grass,
		Rock,
		Wood,
		Metal,
		UndelCaverns,
		MucoraFortress
	}

	private EventReference walkingEvent;

	private EventReference landingEvent;

	private GroundMaterialType lastGroundMaterial;

	private const string AttackParameterName = "Attack Count";

	private const string NarrationParameterName = "Local_VoiceIndex";

	private const string WalkingGroundMaterialParameterName = "Walking_Ground Texture";

	private const string LandingGroundMaterialParameterName = "Landing_Ground Texture";

	private const float BGMVolumeOnBonfire = 0.7f;

	private const float FadeDurationBonfire = 2f;

	private EventInstance instanceBonfire;

	private CancellationTokenSource bonfireBGMTokenSource;

	private CancellationTokenSource bonfireAMBTokenSource;

	private const string LOCAL_MOSTERWAVELEVEL = "Local_MonsterWaveLevel";

	private int currentParam;

	private bool isLegendaryCreature;

	private const string CUTSCENE_ASHHILLS = "Cutscene_Ashhills";

	private const string PR_MOLDINCUTSCENE = "pr_MoldinCutscene";

	private const string PR_WORLDSENDPROGRESS = "pr_WorldsEndProgress";

	private const string PR_ENDINGPROGRESS = "pr_EndingProgress";

	private const string PR_TRUEENDING = "pr_Trueending";

	public const string WalkingEventPath = "event:/SFX/Player/Move/SFX_Walking";

	public const string JumpEventPath = "event:/SFX/Player/Move/SFX_Jump";

	public const string LandingEventPath = "event:/SFX/Player/Move/SFX_Landing";

	public const string LandingHardEventPath = "event:/SFX/Player/Move/SFX_Landing_Hard";

	public const string ParkourGrabEventPath = "event:/SFX/Player/Move/SFX_Parkour_Grab";

	public const string ParkourMoveEventPath = "event:/SFX/Player/Move/SFX_Parkour_Move";

	public const string ParkourLandingEventPath = "event:/SFX/Player/Move/SFX_Parkour_Landing";

	public const string RollingEventPath = "event:/SFX/Player/Move/SFX_Rolling";

	public const string PlayerDieEventPath = "event:/SFX/Player/System/SFX_Player_Death";

	public const string ParryEventPath = "event:/SFX/Player/Combat/SFX_Player_Parrying";

	public const string CounterEventPath = "event:/SFX/Player/Combat/SFX_Player_Counter";

	public const string GuardEventPath = "event:/SFX/Player/Combat/SFX_Player_Guard";

	public const string GuardUpEventPath = "event:/SFX/Player/Combat/SFX_Player_Guard_Up";

	public const string GuardBreakEventPath = "event:/SFX/Player/Combat/SFX_Player_Guard_Break";

	public const string HitEventPath = "event:/SFX/Player/Hit/SFX_Hit";

	public const string KnockDownEventPath = "event:/SFX/Player/Hit/SFX_KnockDown";

	public const string HealStartEventPath = "event:/SFX/Player/System/SFX_Heal_Start";

	public const string HealEndEventPath = "event:/SFX/Player/System/SFX_Heal_End";

	public const string HealEffectEventPath = "event:/SFX/Player/System/SFX_Heal_Effect";

	public const string PlayerAttackGroundEventPath = "event:/SFX/Player/Combat/SFX_Player_Attack_Ground";

	public const string HeavyAttackChargePath = "event:/SFX/Player/Combat/SFX_HeavyAttack_Charge";

	public const string HeavyAttackChargeSuccessPath = "event:/SFX/Player/Combat/SFX_HeavyAttack_Charge_Success";

	public const string HeavyAttackChargeLoopPath = "event:/SFX/Player/Combat/SFX_HeavyAttack_Charge_Loop";

	public const string HeavyAttackPath = "event:/SFX/Player/Combat/SFX_HeavyAttack_Attack";

	public const string ChargeAttackEventPath = "event:/SFX/Player/Combat/SFX_Player_Charge_Attack";

	public const string FastDownEventPath = "event:/SFX/Player/Skill/SFX_Player_RapidDescent";

	public const string FastDownEndEventPath = "event:/SFX/Player/Skill/SFX_Player_RapidDescent_Landing";

	public const string FastDownUpgradedEventPath = "event:/SFX/Player/Skill/SFX_Player_PowerDescend";

	public const string FastDownUpgradedEndEventPath = "event:/SFX/Player/Skill/SFX_Player_PowerDescend_Landing";

	public const string BurstChargeEventPath = "event:/SFX/Player/Skill/SFX_Player_Burst_Charge";

	public const string BurstSlashEventPath = "event:/SFX/Player/Skill/SFX_Player_Burst_Slash";

	public const string BurstFinishEventPath = "event:/SFX/Player/Skill/SFX_Player_Burst_Finish";

	public const string DashFlameLoopEventPath = "event:/SFX/Player/Skill/SFX_Player_DodgeSpark_Mark";

	public const string DashFlameBurstEventPath = "event:/SFX/Player/Skill/SFX_Player_DodgeSpark_Burst";

	public const string ParryingAttackXEventPath = "event:/SFX/Player/Combat/SFX_Player_Parrying_Attack_X";

	public const string FeverLevelUpEventPath = "event:/SFX/Player/Fever/SFX_FeverLevelUp";

	public const string FeverLevelDownEventPath = "event:/SFX/Player/Fever/SFX_FeverLevelDown";

	public const string OverheatEventPath = "event:/SFX/Player/System/SFX_Player_Overheat";

	public const string StuckWallEventPath = "event:/SFX/Boss/Charon/SFX_Charon_Rock_Impact";

	public const string ReturnStartEventPath = "event:/SFX/Player/System/SFX_Player_Recall_Start";

	public const string ReturnEndEventPath = "event:/SFX/Player/System/SFX_Player_Recall_End";

	public const string HyperPyronChargePath = "event:/SFX/Player/Skill/SFX_Player_HyperPyron_Charge";

	public const string HyperPyronFlamePath = "event:/SFX/Player/Skill/SFX_Player_HyperPyron_Flame";

	public const string FlameDamageEventPath = "event:/SFX/Player/Hit/SFX_Burn";

	public const string ElectricSignEventPath = "event:/SFX/Player/Hit/SFX_ElectricShock_Sign";

	public const string ElectricBurstEventPath = "event:/SFX/Player/Hit/SFX_ElectricShock_Burst";

	public const string ElectricLoopEventPath = "event:/SFX/Player/Hit/SFX_ElectricShock_Loop";

	public const string SporeLoopEventPath = "event:/SFX/Player/Hit/SFX_SporeInfected";

	public const string DarknessEventPath = "event:/SFX/Player/Hit/SFX_Darkness";

	public const string WetEventPath = "event:/SFX/Player/Hit/SFX_WaterDebuff";

	public const string CursorMoveEventPath = "event:/SFX/UI/Common/SFX_UI_Cursor_Move";

	public const string SubCursorMoveEventPath = "event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move";

	public const string SmallCursorMoveEventPath = "event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor";

	public const string CursorConfirmEventPath = "event:/SFX/UI/Common/SFX_UI_Cursor_Confirm";

	public const string UIInteractionEventPath = "event:/SFX/UI/Common/SFX_UI_Interaction";

	public const string UINegativeEventPath = "event:/SFX/UI/Common/SFX_UI_Common_Negative";

	public const string NegativeMessageEventPath = "event:/SFX/UI/Common/SFX_UI_NegativePopup";

	public const string LastEmberUIPath = "event:/SFX/UI/Ingame/SFX_UI_LastEmber_Feedback";

	public const string NoStaminaEventPath = "event:/SFX/UI/Common/SFX_UI_StaminaLack";

	public const string GetItemEventPath = "event:/SFX/UI/Items/SFX_UI_GetItem";

	public const string GetCardEventPath = "event:/SFX/UI/Items/SFX_UI_GetCard";

	public const string ItemPanelShowEventPath = "event:/SFX/UI/Items/SFX_UI_GetItem_Show";

	public const string ItemPanelHideEventPath = "event:/SFX/UI/Items/SFX_UI_GetItem_Hide";

	public const string WeaponPanelShowEventPath = "event:/SFX/UI/Equip/SFX_UI_Equip_Select";

	public const string WeaponConfirmEventPath = "event:/SFX/UI/Equip/SFX_UI_Equip_Confirm";

	public const string TalkBoardEventPath = "event:/SFX/UI/Common/SFX_UI_DialogSkip";

	public const string ShopBuyEventPath = "event:/SFX/UI/Common/SFX_UI_Shop_Buy";

	public const string LevelUpAppearEventPath = "event:/SFX/UI/Ingame/SFX_UI_Verita_Appear";

	public const string EquipPartEventPath = "event:/SFX/UI/Equip/SFX_UI_Equip_Gearup";

	public const string MapMarkerPlaceEventPath = "event:/SFX/UI/Map/SFX_UI_Map_Marker_Place";

	public const string MapMarkerRemoveEventPath = "event:/SFX/UI/Map/SFX_UI_Map_Marker_Remove";

	public const string ConsumeFoodEventPath = "event:/SFX/UI/Common/SFX_UI_FoodConsume";

	public const string TraitUpEventPath = "event:/SFX/UI/Trait/SFX_UI_TraitPoint_Up";

	public const string TraitDownEventPath = "event:/SFX/UI/Trait/SFX_UI_TraitPoint_Down";

	public const string TraitConfirmEventPath = "event:/SFX/UI/Trait/SFX_UI_TraitPoint_Confirm";

	public const string TraitRotateEventPath = "event:/SFX/UI/Trait/SFX_UI_TraitPoint_Select";

	public const string TraitSwitchSkillEventPath = "event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewSkill";

	public const string TraitSwitchStatusEventPath = "event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus";

	public const string IncreaseStatEventPath = "event:/SFX/UI/System/SFX_UI_IncreaseMaxStat";

	public const string TutorialOpenEventPath = "event:/SFX/UI/System/SFX_UI_Quest_Open";

	public const string TutorialClearEventPath = "event:/SFX/UI/System/SFX_UI_Quest_Success";

	public const string TutorialKeyGuideEventPath = "event:/SFX/UI/Common/SFX_Tutorial_Ignition_KeyGuide";

	public const string LevelUpEventPath = "event:/SFX/UI/Ingame/SFX_UI_Level_UP";

	public const string MessageBoardEventPath = "event:/SFX/UI/Items/SFX_UI_Message_Appear";

	public const string ServicePanelEventPath = "event:/SFX/UI/Common/SFX_UI_ServicePanel";

	public const string GetCoinLongEventPath = "event:/SFX/UI/Ingame/SFX_Coin_Long";

	public const string ButtonActionAppearEventPath = "event:/SFX/UI/Common/SFX_UI_ButtonAction_Appear";

	public const string ButtonActionClearEventPath = "event:/SFX/UI/Common/SFX_UI_ButtonAction_Clear";

	public const string ButtonActionInputEventPath = "event:/SFX/UI/Common/SFX_UI_ButtonAction_Input";

	public const string GameStartEventPath = "event:/SFX/UI/System/SFX_UI_System_GameStart";

	public const string CreatureHitEventPath = "event:/Creature/SFX_Creature_Hit";

	public const string CreatureHitBloodEventPath = "event:/Creature/SFX_Creature_Hit_Blood";

	public const string GroggyEventPath = "event:/SFX/Player/System/SFX_Player_Groggy";

	public const string BossClear = "event:/SFX/Boss/SFX_Boss_Clear_Common";

	public const string StunRegenEventPath = "event:/Creature/SFX_Resistance_Regen";

	public const string MyosoSpearHitEventPath = "event:/SFX/Char/Myoso/SFX_Myoso_SpearHit_Default";

	public const string MyosoSpearBaranEventPath = "event:/SFX/Char/Myoso/SFX_Myoso_SpearHit_Baran";

	public const string MyosoSpearShockEventPath = "event:/SFX/Char/Myoso/SFX_Myoso_SpearHit_Shock";

	public const string ItemDropEventPath = "event:/SFX/Object/ItemBox/SFX_itemBox_Item_Appear";

	public const string RetrieveSoulEventPath = "event:/SFX/Object/SFX_Object_GetSoul";

	public const string BreakableDebrisPath = "event:/SFX/Object/Breakable/SFX_Breakable_Debris";

	public const string BreakableImpactPath = "event:/SFX/Object/Breakable/SFX_Breakable_Impact";

	public const string BreakableWallPath = "event:/SFX/Object/Breakable/SFX_Breakable_Wall";

	public const string ShadowSproutHitPath = "event:/SFX/Object/SFX_Object_Plague_Hit";

	public const string ShadowSproutBreakPath = "event:/SFX/Object/SFX_Object_Plague_Destroy";

	public const string BarrelIgniteEventPath = "event:/SFX/Object/SFX_Object_Bomb_Ignite";

	public const string BarrelExplosionEventPath = "event:/SFX/Object/SFX_Object_Bomb_Explosion";

	public const string ElevatorTriggerPushEventPath = "event:/SFX/Object/Elevator/SFX_Elevator_ButtonPress";

	public const string ElevatorTriggerReleaseEventPath = "event:/SFX/Object/Elevator/SFX_Elevator_ButtonReleased";

	public const string FogRevealWithHiddenEventPath = "event:/SFX/UI/Ingame/SFX_Hidden_Open";

	public const string FogRevealWithAmbushEventPath = "event:/SFX/UI/Ingame/SFX_Ambushed";

	public const string BonfireEventPath = "event:/SFX/Environment/SFX_Bonfire";

	public const string NarrationVoicePath = "event:/SFX/Story/PaleForest/SFX_Story_Born_Voice";

	public const string LocalIndexParameterName = "Local_Index";

	public const string CreatureMaterialParameterName = "Creature_Material";

	public const string FeverLevelParameterName = "FeverLevel";

	public const string StatusEffectParameterName = "StatusEffect";

	public const string OnlyAMBSoundKey = "OnlyAMB";

	public const string SeedKeeperOrgelSoundKey = "SeedKeeperOrgel";

	public const string SeedKeeperOrgelOutsideSoundKey = "SeedKeeperOrgelOutside";

	private const string TycheTreasureSoundKey = "TycheTreasure";

	public const string MucoraMoldinSoundKey = "MucoraMoldin";

	public const string MoldinBanquetSoundKey = "MoldinBanquet";

	public const string HotelSoundKey = "Hotel";

	public const string ChasaCastleSoundKey = "ChasaCastle";

	public const string ChasaRiverSoundKey = "ChasaRiver";

	public const string VeritaTruthSoundKey = "VeritaTruth";

	public const string BossPrimordialFlamePhase2SoundKey = "BossPrimordialFlamePhase2";

	public const string EndingEpilogueSoundKey = "EndingEpilogue";

	public const string CreditSoundKey = "Credit";

	[SerializeField]
	private BGMAsset bgmAsset;

	[SerializeField]
	private AMBAsset ambAsset;

	public const int MaxVolume = 10;

	private readonly Dictionary<SectorName, EventReference> sectorBGMReferences = new Dictionary<SectorName, EventReference>();

	private readonly Dictionary<string, EventReference> specialBGMReferences = new Dictionary<string, EventReference>();

	private readonly Dictionary<string, Dictionary<FieldID, Vector2>> sound3DReferences = new Dictionary<string, Dictionary<FieldID, Vector2>>();

	private readonly List<FieldID> withoutDefautAMBScenes = new List<FieldID>();

	private readonly List<StudioEventEmitter> sfxEmitters = new List<StudioEventEmitter>();

	private EventInstance bgmInstance;

	private EventInstance ambInstance;

	private EventInstance voiceInstance;

	private string last3DSoundKey = string.Empty;

	private bool isSameBgm;

	private FieldID lastFieldId;

	private Vector2 lastPlayerPos;

	private EventInstance last3DBgmInstance;

	private CancellationTokenSource fadeBGMTokenSource;

	private CancellationTokenSource fadeAMBTokenSource;

	private CancellationTokenSource fadeSFXTokenSource;

	private EventInstance defaultAMBInstance;

	private const string DefaultAMBPath = "event:/AMB/AMB_CommonDark";

	private CancellationTokenSource fadeDefaultAMBTokenSource;

	private const string GLOBAL_MASTER_VOLUME_PARAMETER_NAME = "Global_Volume_Master";

	private const string GLOBAL_SFX_VOLUME_PARAMETER_NAME = "Global_Volume_SFX";

	private const string GLOBAL_BGM_VOLUME_PARAMETER_NAME = "Global_Volume_BGM";

	private const string GLOBAL_AMB_VOLUME_PARAMETER_NAME = "Global_Volume_AMB";

	private const string GLOBAL_VO_VOLUME_PARAMETER_NAME = "Global_Volume_VO";

	private const string LOCAL_BGM_VOLUME_PARAMETER_NAME = "Local_Volume_BGM";

	private const string LOCAL_AMB_VOLUME_PARAMETER_NAME = "Local_Volume_AMB";

	private const string LOCAL_SFX_VOLUME_PARAMETER_NAME = "Local_Volume_SFX";

	private static PlayerMover Player => Game.Instance.Player.mover;

	private static SettingData SettingData => Game.Instance.SettingData;

	private static SaveData SaveData => Game.Instance.SaveData;

	public void PlayWalkingEvent()
	{
		if (walkingEvent.IsNull)
		{
			walkingEvent = RuntimeManager.PathToEventReference("event:/SFX/Player/Move/SFX_Walking");
		}
		PlayOneShotWithParameter(walkingEvent, "Walking_Ground Texture", GetGroundMaterial());
	}

	public void PlayLandingEvent()
	{
		if (landingEvent.IsNull)
		{
			landingEvent = RuntimeManager.PathToEventReference("event:/SFX/Player/Move/SFX_Landing");
		}
		PlayOneShotWithParameter(landingEvent, "Landing_Ground Texture", GetGroundMaterial(), forcedOnSceneLoad: true);
	}

	public void PlayAttackEvent(string path, int count)
	{
		EventReference reference = RuntimeManager.PathToEventReference(path);
		PlayOneShotWithParameter(reference, "Attack Count", count);
	}

	public void PlayNarrationEvent(int count)
	{
		EventReference reference = RuntimeManager.PathToEventReference("event:/SFX/Story/PaleForest/SFX_Story_Born_Voice");
		PlayOneShotWithParameter(reference, "Local_VoiceIndex", count);
	}

	private int GetGroundMaterial()
	{
		List<RaycastHit2D> list = new List<RaycastHit2D>();
		ContactFilter2D contactFilter = new ContactFilter2D
		{
			layerMask = ((int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer),
			useLayerMask = true,
			useTriggers = true
		};
		if (Physics2D.Raycast(Player.VisualBottom + 0.5f * Vector2.up, Vector2.down, contactFilter, list, 1f - Player.RelativeVelocity.y * Time.fixedDeltaTime * 6f) == 0)
		{
			return (int)lastGroundMaterial;
		}
		float num = float.NegativeInfinity;
		IGround ground = null;
		foreach (RaycastHit2D item in list)
		{
			if ((item.transform.TryGetComponent<IGround>(out var component) || item.transform.parent.TryGetComponent<IGround>(out component)) && item.point.y >= num)
			{
				num = item.point.y;
				ground = component;
			}
		}
		if (ground != null)
		{
			lastGroundMaterial = ground.GroundMaterial;
		}
		return (int)lastGroundMaterial;
	}

	public void FadeOnBonfire(bool isActive)
	{
		float start;
		float end;
		if (isActive)
		{
			instanceBonfire = PlayOneShot("event:/SFX/Environment/SFX_Bonfire");
			start = 1f;
			end = 0.7f;
		}
		else
		{
			instanceBonfire.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			instanceBonfire.release();
			start = 0.7f;
			end = 1f;
		}
		FadeBGMParameterAsync(start, end, 2f, TaskUtil.RefreshToken(ref bonfireBGMTokenSource)).Forget();
		FadeAMBParameterAsync(start, end, 2f, TaskUtil.RefreshToken(ref bonfireAMBTokenSource)).Forget();
	}

	public void PlayEnemyWaveBGM(BGMType type)
	{
		if (type != BGMType.None)
		{
			PlayBGM($"EnemyWave_{type}");
		}
	}

	public void SetEnemyWaveParameter(int currentIndex, int totalCount)
	{
		int num = 2 * currentIndex / Mathf.Max(totalCount - 1, 1);
		if (currentParam != num)
		{
			currentParam = num;
			bgmInstance.setParameterByName("Local_MonsterWaveLevel", currentParam);
		}
	}

	public void PlayLegendaryCreatureBGM()
	{
		PlayBGM("LegendaryCreature");
		isLegendaryCreature = true;
	}

	public void StopLegendaryCreatureBGM(string soundKey = null)
	{
		isLegendaryCreature = false;
		PlayBGM(soundKey);
	}

	public void SetAshHillsBGMChoir(bool value)
	{
		bgmInstance.setParameterByName("Cutscene_Ashhills", value ? 1 : 0);
	}

	public void ShiftMoldinBGM()
	{
		bgmInstance.setParameterByName("pr_MoldinCutscene", 1f);
	}

	public void SetWorldsEndBgmParameter(bool shouldStopNow = false)
	{
		int worldsEndBgmParameter = SaveData.WanderingData.GetWorldsEndBgmParameter();
		if (worldsEndBgmParameter >= 6)
		{
			if (shouldStopNow)
			{
				StopBGM();
			}
			UniTask.WaitForSeconds(shouldStopNow ? 0f : 15f).GetAwaiter().OnCompleted(delegate
			{
				PlayAMB("WorldsEndComplete");
			});
		}
		bgmInstance.setParameterByName("pr_WorldsEndProgress", worldsEndBgmParameter);
	}

	public void SetEndingBgmProgress(int index)
	{
		bgmInstance.setParameterByName("pr_EndingProgress", index);
	}

	public void SetTrueEnding(bool value)
	{
		bgmInstance.setParameterByName("pr_Trueending", value ? 1 : 0);
	}

	public static (string bgm, string amb) SelectSoundKey(string soundKey)
	{
		int state;
		switch (soundKey)
		{
		case "SeedKeeperOrgel":
		case "SeedKeeperOrgelOutside":
		{
			bool num = SaveData.EnemyFightData.IsBossCleared(BossName.SeedKeeper) && (!SaveData.WorldData.TryGetObjectState(Game.Instance.CurrentFieldID.SectorName, ServiceName.SeedBureauOrgel.ToString(), out state) || state != 1);
			return new ValueTuple<string, string>(item2: (soundKey == "SeedKeeperOrgel") ? soundKey : string.Empty, item1: num ? "SeedKeeperOrgel" : "OnlyAMB");
		}
		case "TycheTreasure":
			return (bgm: (SaveData.EnemyFightData.IsBossCleared(BossName.Tyche) && (!SaveData.WorldData.TryGetObjectState(Game.Instance.CurrentFieldID.SectorName, ServiceName.TycheTreasureRadio.ToString(), out state) || state != 1)) ? "TycheTreasure" : "OnlyAMB", amb: soundKey);
		case "MoldinBanquet":
			return (bgm: (!SaveData.EnemyFightData.IsBossCleared(BossName.Moldin)) ? "MoldinBanquet" : "OnlyAMB", amb: soundKey);
		case "Hotel":
			return (bgm: (!SaveData.WorldData.TryGetObjectState(Game.Instance.CurrentFieldID.SectorName, ServiceName.HotelRadio.ToString(), out state) || state != 1) ? "Hotel" : "OnlyAMB", amb: soundKey);
		default:
			return (bgm: soundKey, amb: soundKey);
		}
	}

	public static string GetVoicePath(string actorName, string voiceKey)
	{
		return "event:/VO/" + actorName + "/" + voiceKey;
	}

	protected override void OnInitialize()
	{
		GetComponent<StudioBankLoader>().Load();
		sectorBGMReferences.Clear();
		foreach (SectorEventInfo sectorEventInfo in bgmAsset.SectorEventInfos)
		{
			sectorBGMReferences.TryAdd(sectorEventInfo.sectorName, sectorEventInfo.eventReference);
		}
		specialBGMReferences.Clear();
		foreach (SpecialEventInfo specialEventInfo in bgmAsset.SpecialEventInfos)
		{
			specialBGMReferences.TryAdd(specialEventInfo.key, specialEventInfo.eventReference);
		}
		sound3DReferences.Clear();
		foreach (Sound3DAttributeInfo sound3DAttributeInfo in bgmAsset.Sound3DAttributeInfos)
		{
			Dictionary<FieldID, Vector2> dictionary = new Dictionary<FieldID, Vector2>();
			foreach (Sound3DPositionInfo item in sound3DAttributeInfo.fieldSourceDict)
			{
				dictionary.TryAdd(item.fieldId, item.sourcePosition);
			}
			sound3DReferences.TryAdd(sound3DAttributeInfo.key, dictionary);
		}
		withoutDefautAMBScenes.Clear();
		withoutDefautAMBScenes.AddRange(ambAsset.WithoutDefaultAMBScenes);
		SetMasterVolume(SettingData.MasterVol);
		SetSFXVolume(SettingData.SoundVol);
		SetBGMVolume(SettingData.MusicVol);
		SetVOVolume(SettingData.VoiceVol);
	}

	public override UniTask OnSceneLoaded(Scene loadedScene)
	{
		sfxEmitters.Clear();
		sfxEmitters.AddRange(UnityEngine.Object.FindObjectsByType<StudioEventEmitter>(FindObjectsInactive.Include, FindObjectsSortMode.None));
		return UniTask.CompletedTask;
	}

	public void PlayBGM(string soundKey = null)
	{
		if (Game.Instance.RuntimeData.IsBGMLessEvent || isLegendaryCreature)
		{
			return;
		}
		if (soundKey == "None" || soundKey == "OnlyAMB")
		{
			StopBGM();
			SetDefaultAMBVolume(2f);
			return;
		}
		if (string.IsNullOrEmpty(soundKey))
		{
			soundKey = Game.Instance.CurrentFieldID.SectorName.ToString();
		}
		EventReference value;
		if (Enum.TryParse<SectorName>(soundKey, ignoreCase: true, out var result))
		{
			if (!sectorBGMReferences.TryGetValue(result, out value))
			{
				DebugLog.SoundLogWarning($"{result}의 BGM이 존재하지 않으므로 음악을 재생하지 않습니다.");
				return;
			}
		}
		else if (!specialBGMReferences.TryGetValue(soundKey, out value))
		{
			DebugLog.SoundLogError(soundKey + "에 해당하는 BGM을 찾을 수 없습니다.");
			return;
		}
		if (bgmInstance.isValid())
		{
			bgmInstance.getDescription(out var description);
			description.getID(out var id);
			if (id == value.Guid)
			{
				Set3DAttribute();
				isSameBgm = true;
				return;
			}
		}
		StopBGM();
		bgmInstance = RuntimeManager.CreateInstance(value.Guid);
		bgmInstance.setParameterByName("Local_Volume_BGM", 2f, ignoreseekspeed: true);
		bgmInstance.start();
		SetDefaultAMBVolume(1f);
		Set3DAttribute();
		isSameBgm = false;
		if (soundKey.Equals(SectorName.WorldsEnd.ToString()))
		{
			SetWorldsEndBgmParameter(shouldStopNow: true);
		}
		bgmInstance.release();
		void Set3DAttribute()
		{
			if (sound3DReferences.TryGetValue(soundKey, out var value2))
			{
				Vector2 value3;
				bool hasPosition = value2.TryGetValue(Game.Instance.CurrentFieldID, out value3);
				last3DBgmInstance = bgmInstance;
				Set3DAttribute_Internal(value3, hasPosition);
				last3DSoundKey = soundKey;
			}
		}
	}

	private void Set3DAttribute_Internal(Vector2 pos, bool hasPosition)
	{
		ATTRIBUTES_3D attributes = new Vector3(pos.x, pos.y, 0f).To3DAttributes();
		int num = (hasPosition ? (-1) : 10000);
		last3DBgmInstance.set3DAttributes(attributes);
		last3DBgmInstance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, num);
		last3DBgmInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, num);
	}

	public void Set3DAttributeOnSceneLoading()
	{
		if (!sound3DReferences.TryGetValue(last3DSoundKey, out var value))
		{
			DebugLog.SoundLogWarning(last3DSoundKey + "에 대응되는 3D 재생 정보가 없습니다!");
			return;
		}
		if (isSameBgm)
		{
			DebugLog.SoundLogWarning("동일한 BGM(" + last3DSoundKey + ")입니다!");
			return;
		}
		if (!value.ContainsKey(lastFieldId) || value.ContainsKey(Game.Instance.CurrentFieldID))
		{
			DebugLog.SoundLogWarning($"{lastFieldId}가 없거나, 현재 씬({Game.Instance.CurrentFieldID})이 있습니다!");
			return;
		}
		last3DBgmInstance.get3DAttributes(out var attributes);
		Vector2 vector = Player.transform.position;
		Vector2 pos = new Vector2(attributes.position.x - lastPlayerPos.x + vector.x, attributes.position.y - lastPlayerPos.y + vector.y);
		Set3DAttribute_Internal(pos, hasPosition: true);
	}

	public void SaveLastInfo(FieldID fieldId, Vector2 playerPos)
	{
		lastFieldId = fieldId;
		lastPlayerPos = playerPos;
	}

	public void StopBGM(bool allowFadeOut = true)
	{
		fadeBGMTokenSource?.Cancel();
		bgmInstance.stop((!allowFadeOut) ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		bgmInstance.release();
	}

	public void FadeInBGM(string soundKey, float duration)
	{
		PlayBGM(soundKey);
		FadeBGMParameterAsync(0f, 1f, duration, TaskUtil.RefreshToken(ref fadeBGMTokenSource)).Forget();
	}

	public void FadeOutBGM(float duration, bool allowFadeOut = true)
	{
		CancellationToken token = TaskUtil.RefreshToken(ref fadeBGMTokenSource);
		int hashCode = token.GetHashCode();
		FadeBGMParameterAsync(1f, 0f, duration, token).GetAwaiter().OnCompleted(delegate
		{
			if (hashCode == fadeBGMTokenSource.Token.GetHashCode() && !fadeBGMTokenSource.Token.IsCancellationRequested)
			{
				StopBGM(allowFadeOut);
				DebugLog.GeneralLog("Stop BGM After Fade Out");
			}
		});
	}

	private async UniTask FadeBGMParameterAsync(float start, float end, float duration, CancellationToken token)
	{
		bgmInstance.setParameterByName("Local_Volume_BGM", start, ignoreseekspeed: true);
		for (float timer = 0f; timer < duration; timer += Time.deltaTime)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			float value = Mathf.Lerp(start, end, timer / duration);
			bgmInstance.setParameterByName("Local_Volume_BGM", value, ignoreseekspeed: true);
			await UniTask.Yield();
		}
		bgmInstance.getParameterByName("Local_Volume_BGM", out var _);
	}

	public void SetFadeBGMVolume(float value, bool useToken = true)
	{
		CancellationToken token = (useToken ? TaskUtil.RefreshToken(ref fadeBGMTokenSource) : default(CancellationToken));
		bgmInstance.getParameterByName("Local_Volume_BGM", out var value2);
		FadeBGMParameterAsync(value2, value, 1f, token).Forget();
	}

	public void PlayAMB(string soundKey, bool withDefault = true)
	{
		if (soundKey == "None")
		{
			DebugLog.SoundLogWarning("Sound Key가 " + soundKey + "이므로 재생하지 않습니다.");
			StopAMB();
			return;
		}
		if (string.IsNullOrEmpty(soundKey) || soundKey == "OnlyAMB")
		{
			soundKey = Game.Instance.CurrentFieldID.SectorName.ToString();
		}
		EventReference eventReference = default(EventReference);
		if (Enum.TryParse<SectorName>(soundKey, ignoreCase: true, out var result))
		{
			bool flag = false;
			foreach (SectorEventInfo sectorEventInfo in ambAsset.SectorEventInfos)
			{
				if (sectorEventInfo.sectorName == result)
				{
					eventReference = sectorEventInfo.eventReference;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				DebugLog.SoundLogWarning($"{result} 섹터 AMB가 존재하지 않으므로 재생하지 않습니다.");
				StopAMB();
				SetDefaultAMB();
				return;
			}
		}
		else
		{
			eventReference = ambAsset.SpecialEventInfos.FirstOrDefault((SpecialEventInfo info) => info.key == soundKey).eventReference;
		}
		if (eventReference.IsNull)
		{
			DebugLog.SoundLogWarning(soundKey + "에 해당하는 AMB가 존재하지 않으므로 재생하지 않습니다.");
			StopAMB();
			SetDefaultAMB();
			return;
		}
		if (ambInstance.isValid())
		{
			ambInstance.getDescription(out var description);
			description.getID(out var id);
			if (id == eventReference.Guid)
			{
				SetDefaultAMB();
				return;
			}
		}
		StopAMB();
		ambInstance = RuntimeManager.CreateInstance(eventReference.Guid);
		ambInstance.setParameterByName("Local_Volume_AMB", 2f, ignoreseekspeed: true);
		ambInstance.start();
		ambInstance.release();
		SetDefaultAMB();
		void SetDefaultAMB()
		{
			if (withDefault && !withoutDefautAMBScenes.Contains(Game.Instance.CurrentFieldID))
			{
				PlayDefaultAMB();
			}
			else
			{
				StopDefaultAMB();
			}
		}
	}

	public void StopAMB()
	{
		fadeAMBTokenSource?.Cancel();
		ambInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		ambInstance.release();
	}

	public void FadeInAMB(float duration, string soundKey = null)
	{
		PlayAMB(soundKey);
		FadeAMBParameterAsync(0f, 1f, duration, TaskUtil.RefreshToken(ref fadeAMBTokenSource)).Forget();
	}

	public void FadeOutAMB(float duration)
	{
		FadeAMBParameterAsync(1f, 0f, duration, TaskUtil.RefreshToken(ref fadeAMBTokenSource)).GetAwaiter().OnCompleted(delegate
		{
			if (!fadeAMBTokenSource.Token.IsCancellationRequested)
			{
				StopAMB();
			}
		});
	}

	private async UniTask FadeAMBParameterAsync(float start, float end, float duration, CancellationToken token)
	{
		ambInstance.setParameterByName("Local_Volume_AMB", start, ignoreseekspeed: true);
		defaultAMBInstance.setParameterByName("Local_Volume_AMB", start, ignoreseekspeed: true);
		float timer = 0f;
		while (timer < duration)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			timer += Time.deltaTime;
			float value = Mathf.Lerp(start, end, timer / duration);
			ambInstance.setParameterByName("Local_Volume_AMB", value, ignoreseekspeed: true);
			defaultAMBInstance.setParameterByName("Local_Volume_AMB", value, ignoreseekspeed: true);
			await UniTask.Yield();
		}
		ambInstance.getParameterByName("Local_Volume_AMB", out var _);
	}

	public void SetFadeAMBVolume(float value, float duration = 1f)
	{
		ambInstance.getParameterByName("Local_Volume_AMB", out var value2);
		FadeAMBParameterAsync(value2, value, duration, TaskUtil.RefreshToken(ref fadeAMBTokenSource)).Forget();
	}

	private void PlayDefaultAMB()
	{
		if (!defaultAMBInstance.isValid())
		{
			defaultAMBInstance = RuntimeManager.CreateInstance("event:/AMB/AMB_CommonDark");
			defaultAMBInstance.setParameterByName("Local_Volume_AMB", 2f, ignoreseekspeed: true);
			defaultAMBInstance.start();
		}
	}

	private void StopDefaultAMB()
	{
		if (defaultAMBInstance.isValid())
		{
			defaultAMBInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			defaultAMBInstance.release();
		}
	}

	public void SetDefaultAMBVolume(float value, float duration = 2f)
	{
		FadeDefaultAMBVolume(value, duration, TaskUtil.RefreshToken(ref fadeDefaultAMBTokenSource)).Forget();
	}

	private async UniTask FadeDefaultAMBVolume(float end, float duration, CancellationToken token)
	{
		defaultAMBInstance.getVolume(out var start);
		float timer = 0f;
		while (timer < duration)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			timer += Time.deltaTime;
			float volume = Mathf.Lerp(start, end, timer / duration);
			defaultAMBInstance.setVolume(volume);
			await UniTask.Yield();
		}
		defaultAMBInstance.getVolume(out var _);
	}

	public void FadeInSFXEmitters(float duration)
	{
		FadeSFXEmittersAsync(0f, 1f, duration, TaskUtil.RefreshToken(ref fadeSFXTokenSource)).Forget();
	}

	public void FadeOutSFXEmitters(float duration)
	{
		FadeSFXEmittersAsync(1f, 0f, duration, TaskUtil.RefreshToken(ref fadeSFXTokenSource)).Forget();
	}

	public void FadeSFXEmitters(float start, float end, float duration)
	{
		FadeSFXEmittersAsync(start, end, duration, TaskUtil.RefreshToken(ref fadeSFXTokenSource)).Forget();
	}

	private async UniTask FadeSFXEmittersAsync(float start, float end, float duration, CancellationToken token)
	{
		foreach (StudioEventEmitter sfxEmitter in sfxEmitters)
		{
			sfxEmitter.SetParameter("Local_Volume_SFX", start, ignoreseekspeed: true);
		}
		float timer = 0f;
		while (timer < duration && !token.IsCancellationRequested)
		{
			timer += Time.deltaTime;
			float value = Mathf.Lerp(start, end, timer / duration);
			foreach (StudioEventEmitter sfxEmitter2 in sfxEmitters)
			{
				sfxEmitter2.SetParameter("Local_Volume_SFX", value, ignoreseekspeed: true);
			}
			await UniTask.Yield();
		}
	}

	public void PlayVoice(string actorName, string voiceKey)
	{
		StopVoice();
		if (!voiceKey.IsNullOrEmpty())
		{
			EventReference eventReference = RuntimeManager.PathToEventReference(GetVoicePath(actorName, voiceKey));
			if (!eventReference.IsNull)
			{
				voiceInstance = RuntimeManager.CreateInstance(eventReference.Guid);
				voiceInstance.start();
				voiceInstance.release();
			}
		}
	}

	public void StopVoice()
	{
		voiceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
		voiceInstance.release();
	}

	public EventInstance PlayOneShot(string path, bool forcedOnSceneLoad = false)
	{
		EventReference eventReference = RuntimeManager.PathToEventReference(path);
		return PlayOneShot(eventReference, forcedOnSceneLoad);
	}

	public EventInstance PlayOneShot(EventReference eventReference, bool forcedOnSceneLoad = false)
	{
		return PlayOneShot_Internal(eventReference, forcedOnSceneLoad);
	}

	private static EventInstance PlayOneShot_Internal(EventReference eventReference, bool forcedOnSceneLoad)
	{
		if (!forcedOnSceneLoad && Game.Instance.CurrentState != GameState.Initialized && !Game.Instance.UIManager.FadeCanvas.IsWhite)
		{
			return default(EventInstance);
		}
		try
		{
			EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference.Guid);
			AttachToPlayerIf3D(eventInstance);
			eventInstance.start();
			eventInstance.release();
			return eventInstance;
		}
		catch (Exception ex)
		{
			DebugLog.SoundLogWarning(ex.Message);
			return default(EventInstance);
		}
	}

	public EventInstance PlayOneShotAttached(string path, Transform attachedTransform, bool forcedOnSceneLoad = false)
	{
		EventReference reference = RuntimeManager.PathToEventReference(path);
		return PlayOneShotAttached(reference, attachedTransform, forcedOnSceneLoad);
	}

	public EventInstance PlayOneShotAttached(EventReference reference, Transform attachedTransform, bool forcedOnSceneLoad = false)
	{
		if (!forcedOnSceneLoad && Game.Instance.CurrentState != GameState.Initialized && !Game.Instance.UIManager.FadeCanvas.IsWhite)
		{
			return default(EventInstance);
		}
		try
		{
			EventInstance eventInstance = RuntimeManager.CreateInstance(reference.Guid);
			RuntimeManager.AttachInstanceToGameObject(eventInstance, attachedTransform);
			eventInstance.start();
			eventInstance.release();
			return eventInstance;
		}
		catch (Exception ex)
		{
			DebugLog.SoundLogWarning(ex.Message);
			return default(EventInstance);
		}
	}

	public void SetMasterVolume(float value)
	{
		SettingData.MasterVol = Mathf.Clamp(Mathf.RoundToInt(value), 0, 10);
		RuntimeManager.StudioSystem.setParameterByName("Global_Volume_Master", value / 10f);
	}

	public void SetSFXVolume(float value)
	{
		SettingData.SoundVol = Mathf.Clamp(Mathf.RoundToInt(value), 0, 10);
		RuntimeManager.StudioSystem.setParameterByName("Global_Volume_SFX", value / 10f);
	}

	public void SetBGMVolume(float value)
	{
		SettingData.MusicVol = Mathf.Clamp(Mathf.RoundToInt(value), 0, 10);
		RuntimeManager.StudioSystem.setParameterByName("Global_Volume_BGM", value / 10f);
		RuntimeManager.StudioSystem.setParameterByName("Global_Volume_AMB", value / 10f);
	}

	public void SetVOVolume(float value)
	{
		SettingData.VoiceVol = Mathf.Clamp(Mathf.RoundToInt(value), 0, 10);
		RuntimeManager.StudioSystem.setParameterByName("Global_Volume_VO", value / 10f);
	}

	public EventInstance PlayOneShotWithParameter(string path, string paramName, int value, bool forcedOnSceneLoad = false)
	{
		EventReference reference = RuntimeManager.PathToEventReference(path);
		return PlayOneShotWithParameter(reference, paramName, value, forcedOnSceneLoad);
	}

	public EventInstance PlayOneShotWithParameter(EventReference reference, string paramName, int value, bool forcedOnSceneLoad = false)
	{
		if (!forcedOnSceneLoad && Game.Instance.CurrentState != GameState.Initialized)
		{
			return default(EventInstance);
		}
		EventInstance result = RuntimeManager.CreateInstance(reference);
		result.setParameterByName(paramName, value);
		result.start();
		result.release();
		return result;
	}

	public EventInstance PlayOneShotAttachedWithSfxLocalVolume(EventReference reference, Transform attachedTransform, float value)
	{
		if (Game.Instance.CurrentState != GameState.Initialized)
		{
			return default(EventInstance);
		}
		EventInstance eventInstance = RuntimeManager.CreateInstance(reference);
		RuntimeManager.AttachInstanceToGameObject(eventInstance, attachedTransform);
		eventInstance.setParameterByName("Local_Volume_SFX", value);
		eventInstance.start();
		eventInstance.release();
		return eventInstance;
	}

	private static void AttachToPlayerIf3D(EventInstance eventInstance)
	{
		eventInstance.getDescription(out var description);
		description.is3D(out var is3D);
		if (is3D)
		{
			RuntimeManager.AttachInstanceToGameObject(eventInstance, Game.Instance.Player.transform);
		}
	}
}
