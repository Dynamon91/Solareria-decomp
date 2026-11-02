using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu]
public class PlayerControlParameter : ScriptableObject
{
	private static PlayerControlParameter instance;

	public float explosionCountCoefficient = 10f;

	public float explosionDuration = 0.5f;

	[Header("크리쳐 넉백 파워")]
	[SerializeField]
	public List<PowerByMass> creatureMassKnockBackPowers;

	[Header("Status")]
	public int damageRandomRate;

	public int idleRestFrame;

	[Tooltip("텔레포트 버튼 누른 후 모션 나올 때까지 대기하는 시간(초)")]
	public float teleportPredelay;

	[Header("특수 패링")]
	public int heatParryPredelayFrame = 30;

	public int heatParryAttackFrame = 8;

	public int heatParryCounterFrame = 4;

	[Header("차지 어택")]
	[SerializeField]
	private float chargeStartTime;

	[SerializeField]
	private float heavyAttackChargeDuration;

	public float heavyAttackChargeStaminaPerSec;

	public int chargeAttackFrame = 20;

	public int chargePostdelayFrame = 40;

	public int chargeCounterFrame = 8;

	public int counterPaleBlowStunCount;

	public int chargeMoveSpeed;

	public int earlyChargeAcceptFrame;

	[Space(20f)]
	[Header("Move")]
	[SerializeField]
	private int groundAccel;

	[SerializeField]
	private int airAccel;

	[SerializeField]
	private int runSpeed;

	[SerializeField]
	private float runHorizontalInputThreshold;

	[SerializeField]
	private float walkHorizontalInputThreshold;

	[Header("Falling")]
	[SerializeField]
	private float originGravity;

	[SerializeField]
	private int terminalSpeed;

	[SerializeField]
	private int landingHardSpeedThreshold;

	[Space(20f)]
	[Header("Guard")]
	[Tooltip("가드 입력 시점부터 패링 가능한 최대 프레임 수")]
	[SerializeField]
	private int parryAcceptFrame;

	[Tooltip("방어 중 이동속도")]
	[SerializeField]
	private int guardMoveSpeed;

	[SerializeField]
	private int guardPostdelayFrame;

	[FormerlySerializedAs("parryKeepFrame")]
	[SerializeField]
	private int autoParryFrame;

	[SerializeField]
	private int autoGuardFrame;

	[SerializeField]
	private int guardKnockbackFrame;

	public int parryKnockbackFrame;

	public int guardEarlyAcceptFrame = 5;

	public int parryLockFrame = 12;

	[Header("Hit")]
	public int moveAcceptFrameOnHit = 10;

	[Tooltip("기본 피격 시 넉백 시간")]
	[SerializeField]
	private int knockBackFrame;

	[Tooltip("기본 피격 시 넉백 가가속도 (아닌 것 같음. 뭔지 확인 필요)")]
	[SerializeField]
	private int knockBackAccelJerk;

	[SerializeField]
	private Vector2 knockDownVelocity;

	[Tooltip("넘어짐 가가속도")]
	[SerializeField]
	private int knockDownAccelJerk;

	[Header("공격 튕겨남")]
	[Tooltip("공격 튕겨남 상태 프레임 수")]
	[SerializeField]
	private int stuckFrame;

	[Tooltip("공격 튕겨남 후딜 프레임 수")]
	[SerializeField]
	private int stuckPostdelayFrame;

	[Tooltip("공격 튕겨남 넉백 속도")]
	[SerializeField]
	private Vector2 stuckKnockBackVelocity;

	[Space(20f)]
	[Header("파이론 액션")]
	public int parryActionSpeed = 80;

	[Tooltip("패링 액션 중 플레이어 행동 프레임 수")]
	public AttackFrame parryActionMovingFrame;

	[Header("강 파이론 액션")]
	public AttackFrame parryHeavyAttackFrame;

	[Space(20f)]
	[Header("버스트")]
	[SerializeField]
	private int executionEnabledDistance;

	[SerializeField]
	private int daggerExecutionSpeed;

	[Tooltip("베고 난 후 실제 타격까지 대기 프레임 수")]
	[SerializeField]
	private int daggerExecutionDelayFrame;

	public int daggerExecutionPostdelayFrame;

	[Space(20f)]
	[Header("Attack")]
	[SerializeField]
	private int earlyAttackInputAcceptFrame;

	[Tooltip("스킬 사용 후 다른 스킬 입력을 확인하는 보정 프레임 수")]
	[SerializeField]
	private int postInputCheckFrame;

	[Space(20f)]
	[Header("지상 공격")]
	[SerializeField]
	private int continuousAttackAcceptFrame;

	[Tooltip("지상 공격 이후 이동 보정 프레임 수")]
	[SerializeField]
	private int postMovingBlockFrame;

	[SerializeField]
	private int movingOnAttackSpeed;

	[Header("지상 공격 - 단검")]
	public List<AttackFrame> daggerGroundFrames;

	public int daggerGroundSpeed;

	[Space(20f)]
	[Header("공중 공격")]
	public float airAttackStamina;

	public AttackFrame daggerAirFrame;

	public int daggerAirSecondPostdelay;

	[SerializeField]
	private Vector2 daggerAirFirstVelocity;

	[SerializeField]
	private Vector2 daggerAirSecondVelocity;

	public int daggerAirAttackCycle = 4;

	[Space(20f)]
	[Header("빠른 하강")]
	public int fastDownSpeed;

	public int fastDownPostdelayFrame;

	public int fastDownHardPostdelayFrame;

	public int fastDownHardKnockbackPower;

	public int fastDownStamina;

	[Header("화염 발사")]
	[Tooltip("화염 구체 이동속도")]
	public float flameSpeed;

	[Tooltip("화염 구체 이동 최대거리")]
	public float flameMaxDistance;

	public float flameMinRadius;

	public float flameMaxRadius;

	[Tooltip("화염 발사 최대 차징 시간")]
	public float flameMaxChargeDuration;

	[Tooltip("화염 발사 최대 입력 시간")]
	public float flameMaxInputDuration;

	public float flameDefaultStamina;

	public float flameStaminaPerSecond;

	[Header("스킬 - Ash Break")]
	[SerializeField]
	private int ashBreakStamina;

	[SerializeField]
	private Vector2 ashBreakVelocity;

	public int ashBreakPostdelayFrame = 10;

	[Header("스킬 - Ash Blast")]
	[SerializeField]
	private int comboSkillStamina;

	[SerializeField]
	private int comboSkillSpeed;

	[Tooltip("콤보 스킬 연속 공격 입력 허용 프레임 수")]
	[SerializeField]
	private int comboSkillContinuousInputFrame;

	[SerializeField]
	private List<AttackFrame> comboSkillFrames;

	[Space(20f)]
	[Header("Jump")]
	[SerializeField]
	private Vector2 jumpVelocity;

	[SerializeField]
	private int earlyJumpInputAcceptFrame;

	[SerializeField]
	private int lateJumpAcceptFrame;

	[SerializeField]
	private int parkourSpeed;

	[SerializeField]
	private int groggyJumpSpeed;

	[SerializeField]
	private int highJumpSpeedForSolinLab;

	public int jumpMinFrame;

	[Header("회피")]
	[SerializeField]
	private int rollingSpeed;

	[SerializeField]
	private int earlyRollingInputAcceptFrame;

	public int earlyRollingInputAcceptFrameOnHit;

	[SerializeField]
	private int rollingPredelayFrame;

	[SerializeField]
	private int rollingActionFrame;

	[SerializeField]
	private int rollingPostdelayFrame;

	public int rollingDodgeEndFrame;

	public int rollingCoolTimeFrame = 40;

	[Header("공중 회피")]
	[SerializeField]
	private int jumpDashSpeed;

	[Space(20f)]
	[Header("Sight")]
	[Tooltip("Idle 상태에서 이 값 이상의 시야조작이 들어와야 이동합니다.")]
	[SerializeField]
	private float moveSightInputThreshold;

	[Tooltip("위/아래 방향키가 이 시간 이상으로 들어오면 애니메이션을 전환합니다.")]
	[SerializeField]
	private float lookUpDownMinimumInputDuration;

	[Tooltip("애니메이션이 전환된 후 이 시간 이상으로 조작키가 유지되면 시야를 옮깁니다.")]
	[SerializeField]
	private float moveSightHoldingDuration;

	[Header("Stamina")]
	[SerializeField]
	private int rollingStamina;

	[SerializeField]
	private int staminaRegainFrame;

	[SerializeField]
	private int jumpDashStamina;

	[Header("미사용 스킬 Blitz / Heavy Blitz")]
	[SerializeField]
	private float blitzDefaultStamina;

	[SerializeField]
	private float blitzChargeStaminaPerSecond;

	[SerializeField]
	private float heavyBlitzChargeDuration;

	[SerializeField]
	public Vector2 heavyBlitzVelocity;

	public int heavyBlitzActionFrame = 40;

	[SerializeField]
	private List<int> heavyBlitzAttackFrames = new List<int> { 18, 26, 35 };

	public int blitzPredelayFrame = 5;

	[SerializeField]
	private List<int> blitzAttackFrames = new List<int> { 6, 8 };

	[Header("감전")]
	[SerializeField]
	private int shockMaxCount;

	[SerializeField]
	private int shockDuration;

	[SerializeField]
	private int ratioPerShockCount;

	[Header("포자")]
	[SerializeField]
	private int sporeMaxCount = 5;

	[SerializeField]
	private int darknessDamagePerCount = 1;

	[Header("크리쳐 신규 AI 설정")]
	[SerializeField]
	public int creatureDistancePenalty;

	[SerializeField]
	public int creatureRepeatPenalty;

	[Header("보스 관련")]
	[SerializeField]
	private float expRateOnKilledByBoss;

	[SerializeField]
	private float primordialFlame2FixedDamage = 10f;

	public static PlayerControlParameter Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Resources.Load<PlayerControlParameter>("Player Control Parameter");
			}
			return instance;
		}
	}

	public IReadOnlyList<AttackFrame> ComboSkillFrames => comboSkillFrames;

	[field: SerializeField]
	public AttackFrame JumpDashAttackFrame { get; private set; }

	public IReadOnlyList<int> HeavyBlitzAttackFrames => heavyBlitzAttackFrames;

	public IReadOnlyList<int> BlitzAttackFrames => blitzAttackFrames;

	public int ShockMaxCount => shockMaxCount;

	public int ShockDuration => shockDuration;

	public int RatioPerShockCount => ratioPerShockCount;

	public int GroundAccel => groundAccel;

	public int AirAccel => airAccel;

	public int RunSpeed => runSpeed;

	public float RunHorizontalInputThreshold => runHorizontalInputThreshold;

	public float WalkHorizontalInputThreshold => walkHorizontalInputThreshold;

	public float OriginGravity => originGravity;

	public int TerminalSpeed => terminalSpeed;

	public int LandingHardSpeedThreshold => landingHardSpeedThreshold;

	public int StunMultiplier => (int)DataManager.Instance.GetCurrentDifficultyStat("StunMultiplier", Game.Instance.SaveData.Difficulty);

	public int ParryAcceptFrame => (int)DataManager.Instance.GetCurrentDifficultyStat("ParryAcceptFrame", Game.Instance.SaveData.Difficulty);

	public int AutoParryFrame => (int)DataManager.Instance.GetCurrentDifficultyStat("AutoParryGuardFrame", Game.Instance.SaveData.Difficulty);

	public int AutoGuardFrame => (int)DataManager.Instance.GetCurrentDifficultyStat("AutoParryGuardFrame", Game.Instance.SaveData.Difficulty);

	public int GuardMoveSpeed => guardMoveSpeed;

	public int GuardKnockbackFrame => guardKnockbackFrame;

	public int GuardPostdelayFrame => guardPostdelayFrame;

	public int ParkourSpeed => parkourSpeed;

	public int StuckFrame => stuckFrame;

	public int StuckPostdelayFrame => stuckPostdelayFrame;

	public Vector2 StuckKnockBackVelocity => stuckKnockBackVelocity;

	public int ExecutionEnabledDistance => executionEnabledDistance;

	public int DaggerExecutionSpeed => daggerExecutionSpeed;

	public int DaggerExecutionDelayFrame => daggerExecutionDelayFrame;

	public int EarlyAttackInputAcceptFrame => earlyAttackInputAcceptFrame;

	public int ContinuousAttackAcceptFrame => continuousAttackAcceptFrame;

	public int PostInputCheckFrame => postInputCheckFrame;

	public int PostMovingBlockFrame => postMovingBlockFrame;

	public float ChargeStartTime => chargeStartTime;

	public int MovingOnAttackSpeed => movingOnAttackSpeed;

	public float HeavyAttackChargeDuration => heavyAttackChargeDuration;

	public Vector2 DaggerAirFirstVelocity => daggerAirFirstVelocity;

	public Vector2 DaggerAirSecondVelocity => daggerAirSecondVelocity;

	public float HeavyBlitzChargeDuration => heavyBlitzChargeDuration;

	public float BlitzDefaultStamina => blitzDefaultStamina;

	public float BlitzChargeStaminaPerSecond => blitzChargeStaminaPerSecond;

	public Vector2 AshBreakVelocity => ashBreakVelocity;

	public int AshBreakStamina => ashBreakStamina;

	public int ComboSkillStamina => comboSkillStamina;

	public int ComboSkillSpeed => comboSkillSpeed;

	public int ComboSkillContinuousInputFrame => comboSkillContinuousInputFrame;

	public Vector2 JumpVelocity => jumpVelocity;

	public int EarlyJumpInputAcceptFrame => earlyJumpInputAcceptFrame;

	public int LateJumpAcceptFrame => lateJumpAcceptFrame;

	public int GroggyJumpSpeed => groggyJumpSpeed;

	public int HighJumpSpeedForSolinLab => highJumpSpeedForSolinLab;

	public int RollingSpeed => rollingSpeed;

	public int EarlyDashInputAcceptFrame => earlyRollingInputAcceptFrame;

	public int RollingPredelayFrame => rollingPredelayFrame;

	public int RollingActionFrame => rollingActionFrame;

	public int RollingPostdelayFrame => rollingPostdelayFrame;

	public int JumpDashSpeed => jumpDashSpeed;

	public int KnockBackFrame => knockBackFrame;

	public int KnockBackAccelJerk => knockBackAccelJerk;

	public Vector2 KnockDownVelocity => knockDownVelocity;

	public int KnockDownAccelJerk => knockDownAccelJerk;

	public float MoveSightInputThreshold => moveSightInputThreshold;

	public float MoveSightHoldingDuration => moveSightHoldingDuration;

	public float LookUpDownMinimumInputDuration => lookUpDownMinimumInputDuration;

	public float ExpRateOnKilledByBoss => expRateOnKilledByBoss;

	public int StaminaRegainFrame => staminaRegainFrame;

	public int RollingStamina => rollingStamina;

	public int JumpDashStamina => jumpDashStamina;

	public float PrimordialFlame2FixedDamage => primordialFlame2FixedDamage;

	public int SporeMaxCount => sporeMaxCount;

	public int DarknessDamagePerCount => darknessDamagePerCount;
}
