using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FSM;
using Library;
using SPR.FSM;
using UnityEngine;

namespace SPR;

public class PlayerChargeAttackState : PlayerUpgradeableState, ICounter
{
	private const string ChargeAnim = "fever_charge";

	private const string ChargeLoopAnim = "fever_charge_loop";

	private const string AttackAnim = "charge_attack";

	public const string CounterID = "charge_attack_counter";

	private const string SkillDataID = "charge_attack";

	private string counterID;

	private bool isCharging;

	private int chargeLevel;

	private float staminaUsage;

	private float damageMultiple;

	private List<IDamageable> damaged;

	private HeavyAttackChargeEffect chargingObject;

	private EventInstance chargeStartInstance;

	private EventInstance chargeLoopInstance;

	private PoolingObject chargeEffectObject;

	public PlayerCounterAttack CounterAttack { get; set; }

	private AttackAttribute DefaultAttribute => AttackAttribute.Default | AttackAttribute.HeavyAttack;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public event Action<IDamageable> onAttack = delegate
	{
	};

	public PlayerChargeAttackState(PlayerStateMachine psm)
		: base(psm)
	{
		AddressableHelper.PreloadAssetAsync("HeavyAttack_Charging.prefab").Forget();
		counterID = "charge_attack_counter";
	}

	public void OnParried(IDamageable damageable)
	{
		if (base.IsUpgraded)
		{
			CounterAttack.OnParried(damageable);
			if (!damaged.Contains(damageable))
			{
				AttackAttribute attackAttribute = DefaultAttribute | AttackAttribute.Counter;
				PlayerMover.Attack(new PlayerMover.AttackParameter(damageable, "charge_attack", 0, attackAttribute));
				OnAttack(damageable);
			}
		}
	}

	public int GetStunCount(Enemy enemy, AttackAttribute attackAttribute)
	{
		if (attackAttribute.HasFlag(AttackAttribute.RedFear))
		{
			return PlayerAsyncState.pcp.counterPaleBlowStunCount;
		}
		return 1;
	}

	public void ChangeID(string id)
	{
		counterID = id;
	}

	protected override void OnEnter(IState prevState)
	{
		damaged = new List<IDamageable>();
		PlayerMover.ResetRelativeVelocity();
		if (base.IsUpgraded)
		{
			CounterAttack = new PlayerCounterAttack(PlayerMover, counterID);
		}
		chargeLevel = PlayerStat.HeatLevel;
		staminaUsage = 0f;
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.GravityFree | StateOption.BlockStaminaRegain | StateOption.Parry, active: false);
		PlayerMover.ResetRelativeVelocity();
		isCharging = false;
		StopFX();
		PlayerState.PlayerController.PlayerPart.OnChargeEndOrCancel();
	}

	protected override void OnFixedUpdate(float fdt)
	{
		if (isCharging && psm.RollingInputItem.CanExecute(Time.time))
		{
			psm.RollingInputItem.Execute();
		}
		else if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
		}
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		isCharging = true;
		chargeLevel = await GetChargeLevelAsync(token);
		isCharging = false;
		if (token.IsCancellationRequested)
		{
			return;
		}
		if (chargeLevel == -1)
		{
			float time = Time.time;
			if (psm.BasicAttackInputItem.CanExecute(time))
			{
				psm.BasicAttackInputItem.Execute();
			}
			else
			{
				psm.ChangeState(psm.IdleState);
			}
			return;
		}
		damageMultiple = (PlayerState.PlayerController.PlayerPart.TryGetChargeAttackMultiple(staminaUsage, out var multiple) ? multiple : 1f);
		await AttackAsync(token);
		if (!token.IsCancellationRequested)
		{
			psm.ChangeState(psm.IdleState);
		}
	}

	private async UniTask AttackAsync(CancellationToken token)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: true);
		if (base.IsUpgraded)
		{
			psm.SetStateOptionActive(StateOption.Parry, active: true);
		}
		Vector3 startPos = PlayerMover.transform.position;
		PlayerMover.SetAnimation("charge_attack", loop: false);
		PlayerMover.ApplyRelativeVelocity(PlayerMover.FacingDirection.GetSignX() * PlayerAsyncState.pcp.chargeMoveSpeed, 0f, faceJump: true);
		PlayerMover.ClearDamagedList();
		int frameCount = 0;
		while (!token.IsCancellationRequested)
		{
			if (frameCount == PlayerAsyncState.pcp.chargeCounterFrame && base.IsUpgraded)
			{
				psm.SetStateOptionActive(StateOption.Parry, active: false);
			}
			if (frameCount >= PlayerAsyncState.pcp.chargeCounterFrame)
			{
				PlayerMover.HandleAttackHitBox("charge_attack", 0, DefaultAttribute, damageMultiple);
				PlayerMover.HandleAttackHitBoxFrom(startPos, "charge_attack", 0, DefaultAttribute, null, random: true, damageMultiple);
			}
			foreach (IDamageable item in PlayerMover.Damaged)
			{
				if (!damaged.Contains(item))
				{
					OnAttack(item);
				}
			}
			if (frameCount >= PlayerAsyncState.pcp.chargeAttackFrame)
			{
				PlayerMover.ResetRelativeVelocity();
				break;
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			frameCount++;
		}
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.chargePostdelayFrame, token);
	}

	private void OnAttack(IDamageable damageable)
	{
		damaged.Add(damageable);
		if (base.IsUpgraded)
		{
			CounterAttack.TryCounterAttack(damageable);
		}
		this.onAttack?.Invoke(damageable);
	}

	private async UniTask<int> GetChargeLevelAsync(CancellationToken token)
	{
		PlayChargeFX();
		psm.SetStateOptionActive(StateOption.BlockStaminaRegain, active: true);
		PlayerState.PlayerController.PlayerPart.OnChargeStart();
		PlayerMover.SetAnimation("fever_charge", loop: false);
		PlayerMover.AddAnimation("fever_charge_loop");
		ChargeAttackInputItem inputItem = psm.ChargeAttackInputItem;
		int result = -1;
		staminaUsage = 0f;
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				return 0;
			}
			if (!inputItem.IsHolding)
			{
				break;
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			int chargingLevel = GetChargingLevel(inputItem.HoldDuration);
			if (chargingLevel > result)
			{
				result = chargingLevel;
				PlaySuccessFX();
			}
			if (PlayerState.PlayerStat.GetStat<Stamina>().Current > 0f)
			{
				float num = PlayerAsyncState.pcp.heavyAttackChargeStaminaPerSec * Time.fixedDeltaTime;
				PlayerState.PlayerStat.GetStat<Stamina>().AddStamina(0f - num);
				staminaUsage += num;
			}
		}
		PlayerState.PlayerController.PlayerPart.OnChargeEndOrCancel();
		psm.SetStateOptionActive(StateOption.BlockStaminaRegain, active: false);
		StopFX();
		return result;
	}

	private static int GetChargingLevel(float holdDuration)
	{
		if (holdDuration < PlayerAsyncState.pcp.HeavyAttackChargeDuration)
		{
			return -1;
		}
		return PlayerStat.HeatLevel;
	}

	private void PlayChargeFX()
	{
		chargeEffectObject = Game.Instance.ObjectPoolManager.GetObject<PoolingObject>("Fever_Readyloop.prefab", PlayerMover.transform.position, PlayerMover.transform);
		chargeStartInstance = SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_HeavyAttack_Charge");
		chargeLoopInstance = SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_HeavyAttack_Charge_Loop");
	}

	private void PlaySuccessFX(int level = 1)
	{
		PlayerMover.EnableVFX($"fever_charge_{level}");
		SoundManager.PlayOneShotWithParameter("event:/SFX/Player/Combat/SFX_HeavyAttack_Charge_Success", "ChargeLevel", level - 1);
	}

	private void StopFX()
	{
		if (chargeEffectObject != null)
		{
			ParticleSystem p = chargeEffectObject.transform.GetChild(0).GetComponent<ParticleSystem>();
			ParticleSystem.MainModule main = p.main;
			main.loop = false;
			UniTask.WaitUntil(() => !p.isEmitting).GetAwaiter().OnCompleted(delegate
			{
				main.loop = true;
				chargeEffectObject.ReturnToPool();
			});
		}
		if (chargeStartInstance.isValid())
		{
			chargeStartInstance.stop(STOP_MODE.ALLOWFADEOUT);
		}
		if (chargeLoopInstance.isValid())
		{
			chargeLoopInstance.stop(STOP_MODE.ALLOWFADEOUT);
		}
	}
}
