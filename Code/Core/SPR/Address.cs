using SPR.UI.Map;
using SPR.UI.Menu;
using SPR.UI.Tutorial;
using StaticData;

namespace SPR;

public static class Address
{
	public const string GamePrefab = "Game";

	public const string Player = "Assets/Prefabs/Player.prefab";

	public const string LocaleFontSet = "Locale Font Set";

	public const string MoomooVine = "MumuVine_B.prefab";

	public const string LandingDust = "Landing dust.prefab";

	public const string JumpEffect = "Jump.prefab";

	public const string TempFlameBossMaterial = "Temp_Blitz Flame Boss.mat";

	public const string TempHeavyBlitzFlame = "Temp_Heavy Blitz Flame.prefab";

	public const string HeavyAttackEffect = "HeavyAttack_Charging.prefab";

	public const string FeverReadyLoop = "Fever_Readyloop.prefab";

	public const string FlameBullet = "Flame Bullet.prefab";

	public const string FlameBulletEnd = "Flame Bullet_End.prefab";

	public const string RollingAttackFlame = "Rolling Attack Flame.prefab";

	public const string GroundFlame = "Ground Flame.prefab";

	public const string FeverFlame = "Fever Flame.prefab";

	public const string ExecutionFlame = "Execution Flame.prefab";

	public const string PlayerShockwave = "Player Overheat Wave.prefab";

	public const string PlayerFlameWave = "Player Flame Wave.prefab";

	public const string PlayerReturnStart = "Player_Return_Start.prefab";

	public const string PlayerReturnEnd = "Player_Return_End.prefab";

	public const string TalkVcam = "Talk Vcam.prefab";

	public const string UnknownItemID = "Unknown";

	public const string UnknownSpriteAddress = "ItemSprite/Unknown";

	public const string WarmhornItemID = "Warmhorn";

	public const string WarmhornSpriteAddress = "ItemSprite/Warmhorn/Warmhorn";

	public const string DebugAchievementUI = "UI/AchievementToast";

	public const string TrapDoor = "TrapDoor.prefab";

	public const string TrapDoorFixButton = "TrapDoorFixButton.prefab";

	public const string DefenseObject = "Defense Object.prefab";

	public const string Money = "Money.prefab";

	public const string PlayerDropItem = "Player Drop Item.prefab";

	public const string FallingRock = "Falling Rock.prefab";

	public const string EarthfordBranchDoor = "Earthford Branch Door.prefab";

	public const string ItemObject = "ItemObject.prefab";

	public const string SeedArcherArrow = "Seed Archer Arrow.prefab";

	public const string SpearmanJavelin = "Spearman Javelin.prefab";

	public const string AcapultSign = "Acapult Sign.prefab";

	public const string BurstIcon = "Burst Icon.prefab";

	public const string InfectedWarriorFragment = "Infected Warrior Fragment.prefab";

	public const string WebbedLizardBubble = "WebbedLizardBubble.prefab";

	public const string RoyalGuardWaterBomb = "Royal Guard Water Bomb.prefab";

	public const string WaterDragonWave = "Water Dragon Wave.prefab";

	public const string WaterDragonCanon = "Water Dragon Canon.prefab";

	public const string CharonRock = "Charon Rock.prefab";

	public const string DarkChasaBladeBeam = "DarkChasa_BladeBeam.prefab";

	public const string DarkChasaShadow = "Dark Chasa_Shadow.prefab";

	public const string ChasaKnife = "ChasaKnife.prefab";

	public const string ChasaShadow = "Chasa_Shadow.prefab";

	public const string VeritaHorizontalArrow = "Verita Horizontal Arrow.prefab";

	public const string VeritaVerticalArrow = "Verita Vertical Arrow.prefab";

	public const string VeritaSpaceArrow = "Verita Space Arrow.prefab";

	public const string VeritaGiantArrow = "Verita Giant Arrow.prefab";

	public const string VeritaShockwave = "Verita Shockwave.prefab";

	public const string VeritaOneShotShockwave = "Verita OneShot Shockwave.prefab";

	public const string VeritaExplosionWave = "Verita Explosion Wave.prefab";

	public const string VeritaFlame = "Verita Flame.prefab";

	public const string VeritaFlameBullet = "Verita Flame Bullet.prefab";

	public const string VeritaArrowPortal = "Verita Arrow Portal.prefab";

	public const string VeritaSighEffect = "Wave Seed.prefab";

	public const string SoowooRisingWater = "Soowoo Rising Water.prefab";

	public const string GatekeeperFlameField = "Gatekeeper Flame Field.prefab";

	public const string GatekeeperFlameEnergy = "Gatekeeper Flame Energy.prefab";

	public const string PrimordialFlameSword = "Primordial Flame_Sword.prefab";

	public const string PrimordialFlameFlameField = "Primordial Flame_Flame Field.prefab";

	public const string PrimordialFlamePillarOfFlame = "Primordial Flame_Pillar Of Flame.prefab";

	public const string PrimordialFlameTheEndWave = "Primordial Flame_TheEndWave.prefab";

	public const string PrimordialFlameFlame = "Primordial Flame2_Flame.prefab";

	public const string CreatureCanvas = "Creature Canvas.prefab";

	public const string FieldInteractCanvas = "FieldInteractCanvas.prefab";

	public const string FieldServiceCanvas = "Field Service Canvas.prefab";

	public const string TraitCardBoard = "UI/TraitCardBoard.prefab";

	public const string StoreItemBoard = "Store Item Board.prefab";

	public const string InventoryCell = "Inventory Cell.prefab";

	public const string IngredientCell = "Ingredient Cell.prefab";

	public const string MapIndicator = "Map Indicator.prefab";

	public const string MapNormalMarkerIcon = "Map Normal Marker Icon.prefab";

	public const string MemoryDataCell = "Memory Data Cell.prefab";

	public const string MemoryAreaTitle = "Memory Area Title.prefab";

	public const string TutorialDataCell = "Tutorial Data Cell.prefab";

	public const string PlayerEffectCanvas = "Player Effect Canvas.prefab";

	public const string ItemGridCell = "Item Grid Cell.prefab";

	public const string ItemRowCell = "Item Row Cell.prefab";

	public const string PartCell = "Part Cell.prefab";

	public const string VeritaCutscene = "Verita Cutscene Canvas.prefab";

	public const string EnemyDamageEffect = "VFX/Enemy_Damaged.prefab";

	public const string EnemyIndirectDamageEffect = "VFX/Enemy_Damaged_Indirect.prefab";

	public const string EnemyBlackFogEffect = "VFX/Enemy_BlackFog.prefab";

	public const string EnemyDamageCircleEffect = "VFX/Hit_Circle.prefab";

	public const string EnemyDamageFourthEffect = "VFX/Enemy_Damaged_4.prefab";

	public const string VFXDaggerHeavyAttackHit = "VFX/Dagger_HeavyAttack_Hit.prefab";

	public const string VFXPlayerDeath = "VFX/Player_Death.prefab";

	public const string VFXPlayerStiffen = "VFX/Player_Stiffen.prefab";

	public const string VFXPlayerStiffenDamage = "VFX/Player_Stiffen_Damage.prefab";

	public const string VFXDownLandingDust = "VFX/Knockback_LandingDust.prefab";

	public const string VFXShockwave = "Attack_Distortion.prefab";

	public const string VFXCobbleFalling = "VFX/Charon_CobbleFalling.prefab";

	public const string VFXRockDust = "VFX/Charon_RockDust.prefab";

	public const string VFXRockBreak = "VFX/Charon_RockBreak.prefab";

	public const string VFXRockHit = "VFX/Charon_RockHit.prefab";

	public const string VFXGuard = "VFX/Stone_Guard";

	public const string VFXGroggyFlame = "VFX/Blitz_FireFloor.prefab";

	public const string TempHitBomb = "Hit_Bomb.prefab";

	public const string TempBoom = "Temp_Boom.prefab";

	public const string BaranTornado = "VFX/Baran_Tornado.prefab";

	public const string ChasaTeleportIn = "VFX/Chasa_Teleport_In.prefab";

	public const string ChasaTeleportOut = "VFX/Chasa_Teleport_Out.prefab";

	public const string MoomooStompDust = "VFX/Moomoo_StompDust.prefab";

	public const string RedFear = "VFX/RedFear.prefab";

	public const string DeathBurn = "Death Burn.prefab";

	public const string OverheatBurstExplosion = "Overheat Burst Explosion.prefab";

	public const string WaterDebuffEffect = "Water_Debuff.prefab";

	public const string DashAttackEffect = "Dash Attack Effect";

	public const string AshTrap1 = "Ash Trap 1";

	public const string AshTrap2 = "Ash Trap 2";

	public const string ShieldLoop = "Shield_Loop";

	public const string ShieldEnd = "Shield_End";

	public const string PrimordialFlameSwordDisappear = "vfx_Primordial Flame_Phase2_Sword_Disappear.prefab";

	public const string DebugAttackArea = "Debug/DebugAttackArea.prefab";

	public const string DebugCollider = "Debug/DebugCollider.prefab";

	public const string IngredientLabel = "ItemSprite/Ingredient";

	public const string FoodLabel = "ItemSprite/Food";

	public static string ItemSprite(string itemID)
	{
		if (!(itemID == "Unknown"))
		{
			if (itemID == "Warmhorn")
			{
				return "ItemSprite/Warmhorn/Warmhorn";
			}
			ItemData itemData = Game.DataManager.GetItemData(itemID);
			if (itemData == null)
			{
				return string.Empty;
			}
			_ = itemData.ItemType;
			return $"ItemSprite/{itemData.ItemType}/{itemID}";
		}
		return "ItemSprite/Unknown";
	}

	public static string IconSprite(MenuCategory menuCategory, string iconID)
	{
		return $"{menuCategory}/{iconID}";
	}

	public static string TraitSprite(string cardName)
	{
		return "TraitCard/" + cardName;
	}

	public static string FoodObjectSprite(string foodName)
	{
		return "FoodObject/" + foodName;
	}

	public static string StatusEffectSprite(StatusEffectType effectType, int stack = 1, bool active = false)
	{
		if (stack > 3)
		{
			stack = 3;
		}
		if (effectType != StatusEffectType.Electric)
		{
			return $"StatusEffect/{effectType}";
		}
		return string.Format("StatusEffect/{0}_{1}{2}", effectType, stack, active ? "_active" : "");
	}

	public static string TutorialExample(ActionName actionName, Language language = Language.None)
	{
		string arg = "";
		if (actionName == ActionName.Level)
		{
			language = ((language == Language.None) ? Game.CurrentLanguage : language);
			arg = $"_{language}";
		}
		return $"Tutorial_{actionName}{arg}";
	}

	public static string MapMarkerSprite(int type)
	{
		return "ItemSprite/MapMarker/" + MapMarkerIcon.GetMarkerName(type);
	}

	public static string EntranceLight(SectorName sectorName)
	{
		return $"Entrance Light_{sectorName switch
		{
			SectorName.AbandonedTunnels => sectorName, 
			SectorName.Earthford => sectorName, 
			SectorName.SilentRuins => sectorName, 
			_ => SectorName.SilentRuins, 
		}}.prefab";
	}

	public static string HeatSkillVideoClip(HeatSkillType skillType)
	{
		return $"Tutorial_{skillType}";
	}

	public static string DifficultyIconSprite(Difficulty difficulty)
	{
		return $"Difficulty_{difficulty}";
	}

	public static string LandmarkSprite(FieldID fieldID)
	{
		return "Save_" + fieldID.ToString();
	}
}
