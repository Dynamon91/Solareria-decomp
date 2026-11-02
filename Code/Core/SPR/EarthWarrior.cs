using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using LapinLibrary;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using SPR.UI.Tutorial;
using Spine;
using UnityEngine;

namespace SPR;

public class EarthWarrior : Creature
{
	[Header("확률")]
	[SerializeField]
	private int slashChance;

	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<EarthWarrior> throwBoomerang;

	[SerializeField]
	private GenericAttackSequence<EarthWarrior> slash;

	[SerializeField]
	private GenericAttackSequence<EarthWarrior> charge;

	[SerializeField]
	private GenericAttackSequence<EarthWarrior> parryCharge;

	[Header("References")]
	[SerializeField]
	private Transform weaponStartTransform;

	[SerializeField]
	private Transform weaponThrowTransform;

	[SerializeField]
	private EventReference idleVoiceEvent;

	private EarthWarriorWeapon weapon;

	private CancellationTokenSource hitPlatformCanceller;

	private EventInstance idleVoiceInstance;

	public override string Name => "EarthWarrior";

	public bool WaveSpawning { get; set; }

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(throwBoomerang, () => new List<AttackSequence> { throwBoomerang });
		AttackPatterns.TryAdd(slash, () => new List<AttackSequence> { slash });
		AttackPatterns.TryAdd(charge, () => new List<AttackSequence> { charge });
		AddAction(ActionTiming.Begin, "ThrowWeapon", delegate(Parameter[] p)
		{
			ThrowWeapon(p[0].Int, p[1].Int).Forget();
		});
		AddAction(ActionTiming.Begin, "Charge", delegate(Parameter[] p)
		{
			Charge(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "ReturnWeapon", ReturnWeapon);
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		return base.GetAttackSequences();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { throwBoomerang, slash };
	}

	protected override void OnDie()
	{
		base.OnDie();
		weapon?.ReturnToPool();
		DOTween.Kill(base.transform);
	}

	protected override void OnFixedUpdate()
	{
		if (!WaveSpawning && base.PathNodesList != null)
		{
			if (PathFinder.FindNode(base.PathNodesList, base.Position) == null && !(base.StateMachine.CurrentState is CreatureFallingState))
			{
				RaycastHit2D raycastHit2D = Physics2D.Raycast(VisualBottom, Vector2.down, float.PositiveInfinity, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
				base.StateMachine.ChangeState(new CreatureFallingState(this, raycastHit2D.point));
			}
			PlayIdleVoice();
		}
	}

	public override void OnEnterBounceState()
	{
		if (base.BounceAnim == "parry_rush")
		{
			base.StateMachine.CurrentState.CastObject<EnemyBounceState>().NextState = new AttackState(this, () => new List<AttackSequence> { parryCharge }, forceAttack: true);
		}
		DOTween.Kill(base.transform);
	}

	public override void OnLandingComplete(TrackEntry track)
	{
		if (!IsDead && track.Animation.Name == "landing")
		{
			if (Game.Instance.SaveData.WanderingData.ShouldShowTutorial(ActionName.Red))
			{
				Game.Instance.UIManager.TutorialCanvas.SetData(ActionName.Red);
				Game.Instance.InputManager.StackControlTarget(Game.Instance.UIManager.TutorialCanvas).Forget();
			}
			SetConnectedPath();
			base.StateMachine.ChangeState(DefaultState);
			WaveSpawning = false;
		}
	}

	private async UniTaskVoid ThrowWeapon(int moveDuration, int returnWaitFrame)
	{
		if (!CheckWeaponHitBeforeThrow())
		{
			weapon = Enemy.PoolManager.GetObject<EarthWarriorWeapon>("Earth Warrior Weapon.prefab", weaponStartTransform.position);
			weapon.SortingOrder = base.SortingOrder + 1;
			DOTween.Kill(weapon.transform);
			weapon.transform.Flip(FacingDirection.GetSignX());
			weapon.FacingDirection = FacingDirection;
			weapon.transform.DOMove(weaponThrowTransform.position, Util.FrameToSeconds(moveDuration)).SetEase(Ease.Linear).OnUpdate(delegate
			{
				weapon.HandleOverlap();
				CheckWeaponHit((weaponThrowTransform.position - weaponStartTransform.position).normalized);
			})
				.OnComplete(weapon.ResetDamageList);
			await UniTask.WaitForSeconds(Util.FrameToSeconds(moveDuration), ignoreTimeScale: false, PlayerLoopTiming.Update, TaskUtil.RefreshToken(ref hitPlatformCanceller));
			await UniTask.WaitForSeconds(Util.FrameToSeconds(returnWaitFrame), ignoreTimeScale: false, PlayerLoopTiming.Update, TaskUtil.RefreshToken(ref hitPlatformCanceller));
			weapon.FacingDirection = FacingDirection.Opposite();
			weapon.transform.DOMove(weaponStartTransform.position, Util.FrameToSeconds(moveDuration)).SetEase(Ease.Linear).OnUpdate(weapon.HandleOverlap)
				.OnComplete(weapon.ReturnToPool);
		}
	}

	private bool CheckWeaponHitBeforeThrow()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(new Vector2(VisualCenter.x, weaponStartTransform.position.y), FacingDirection.ToVector2(), Vector2.Distance(VisualCenter, weaponStartTransform.position), LayerUtility.PlatformLayer);
		if ((bool)raycastHit2D)
		{
			weapon = Enemy.PoolManager.GetObject<EarthWarriorWeapon>("Earth Warrior Weapon.prefab", new Vector2(VisualCenter.x, weaponStartTransform.position.y));
			weapon.SortingOrder = base.SortingOrder + 1;
			DOTween.Kill(weapon.transform);
			weapon.StopRotate(raycastHit2D.point);
			hitPlatformCanceller?.Cancel();
			base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { charge }, forceAttack: true));
			return true;
		}
		return false;
	}

	private void CheckWeaponHit(Vector2 direction)
	{
		RaycastHit2D raycastHit2D = weapon.CheckPlatform(direction);
		if ((bool)raycastHit2D)
		{
			DOTween.Kill(weapon.transform);
			weapon.StopRotate(raycastHit2D.point);
			hitPlatformCanceller.Cancel();
			base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { charge }, forceAttack: true));
		}
	}

	private void Charge(float speed)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(weapon.transform.position, FacingDirection.ToVector2(), float.PositiveInfinity, LayerUtility.PlatformLayer);
		base.transform.DOMoveX(raycastHit2D.point.x + (float)base.DetectPathWidth / 2f * (float)(-FacingDirection.GetSignX()), speed).SetUpdate(UpdateType.Fixed).SetEase(Ease.Linear)
			.SetSpeedBased(isSpeedBased: true)
			.OnUpdate(delegate
			{
				PathFinder.Node node = PathFinder.FindNode(base.Position);
				base.Position = new Vector2(base.Position.x, node.Position.y);
			});
	}

	private void ReturnWeapon()
	{
		weapon.ReturnToPool();
		weapon = null;
	}

	private void PlayIdleVoice()
	{
		idleVoiceInstance.getPlaybackState(out var state);
		bool flag = state != PLAYBACK_STATE.STOPPED;
		IState currentState = base.StateMachine.CurrentState;
		if (currentState is PatrolState || currentState is ChaseState || currentState is AlertState)
		{
			if (!flag)
			{
				idleVoiceInstance = Enemy.SoundManager.PlayOneShotAttached(idleVoiceEvent, base.transform);
			}
		}
		else if (flag)
		{
			idleVoiceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			idleVoiceInstance.release();
		}
	}
}
