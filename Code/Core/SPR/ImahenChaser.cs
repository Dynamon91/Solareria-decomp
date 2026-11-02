using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Cutscene;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class ImahenChaser : SpineObject, IEventListener
{
	public enum Mode
	{
		Stop,
		Chase,
		Boss,
		BossEnd,
		Return
	}

	[Header("속도")]
	[SerializeField]
	private float cutsceneSpeed = 30f;

	[SerializeField]
	private float bossSpeed = 3.2f;

	[SerializeField]
	private float bossEndSpeed;

	[SerializeField]
	private float reverseSpeed = 200f;

	[Header("공격 프레임")]
	[SerializeField]
	private int attackDuration = 60;

	[SerializeField]
	private int attackCycle = 20;

	[SerializeField]
	private int coreMachineAttackCycle = 60;

	[Header("Effect")]
	[SerializeField]
	private ParticleSystem onCapsuleEffect;

	[SerializeField]
	private ParticleSystem onPlatformEffect;

	[SerializeField]
	private ParticleSystem[] reverseFloorEffects;

	[SerializeField]
	private BoxCollider2D capsuleObjectCollider;

	[SerializeField]
	private BoxCollider2D[] platformObjectCollider;

	[Header("Cinemachine")]
	[SerializeField]
	private CinemachineImpulseSource impulseSource;

	[Header("FMOD")]
	[SerializeField]
	private EventReference sawEvent;

	[SerializeField]
	private EventReference reverseEvent;

	[SerializeField]
	private EventReference hitEvent;

	public const string NameID = "Imahen_Chaser";

	private bool isStopped;

	private BoxCollider2D box2D;

	private ImahenCutsceneProducer imahenCutscene;

	private Vector3 returnPosition;

	private bool reverse;

	private EventInstance sawInstance;

	private float speed;

	private FrameTimer coreMachineAttackTimer;

	private bool isRumbling;

	private CancellationTokenSource cts = new CancellationTokenSource();

	public override string Name => "Imahen_Chaser";

	public override Direction FacingDirection => Direction.Right;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private ImahenCutsceneProducer ImahenCutscene => imahenCutscene ?? (imahenCutscene = Game.Instance.FieldController.FindCutsceneProducer<ImahenCutsceneProducer>());

	private static LayerMask AttackLayer => LayerUtility.PlayerLayer;

	private static LayerMask EnemyAttackLayer => (int)LayerUtility.MonsterLayer | (int)LayerUtility.ObjectLayer;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private void Awake()
	{
		base.VfxController.InitializeVFXDictionary();
		coreMachineAttackTimer = new FrameTimer(coreMachineAttackCycle);
	}

	private void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<ImahenChaserUpdateEvent>(this);
		StopSfx();
	}

	private void FixedUpdate()
	{
		HandleOverlap();
		if (isStopped)
		{
			return;
		}
		if (!reverse)
		{
			base.transform.position += Vector3.right * (speed * Time.fixedDeltaTime);
			HandleOverlap();
		}
		else
		{
			base.transform.position = Vector3.MoveTowards(base.transform.position, returnPosition, reverseSpeed * Time.fixedDeltaTime);
			if (base.transform.position == returnPosition)
			{
				reverse = false;
				ParticleSystem[] array = reverseFloorEffects;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
				}
			}
		}
		CheckOverlapCapsule();
		CheckOverlapPlatform();
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is ImahenChaserUpdateEvent imahenChaserUpdateEvent)
		{
			Vector3 nextPosition = new Vector3(imahenChaserUpdateEvent.NewReturnPosition.x, base.transform.position.y);
			switch (imahenChaserUpdateEvent.SwitchMode)
			{
			case Mode.Stop:
				Stop();
				break;
			case Mode.Chase:
				isStopped = false;
				ChangeSpeedAsync(nextPosition, cutsceneSpeed, instant: true).Forget();
				returnPosition = nextPosition;
				break;
			case Mode.Boss:
				isStopped = false;
				isRumbling = false;
				ChangeSpeedAsync(nextPosition, bossSpeed, instant: false).Forget();
				returnPosition = nextPosition;
				break;
			case Mode.BossEnd:
				isStopped = false;
				ChangeSpeedAsync(nextPosition, bossEndSpeed, instant: true).Forget();
				returnPosition = nextPosition;
				break;
			case Mode.Return:
				Stop();
				base.transform.DOMoveX(returnPosition.x, reverseSpeed).SetUpdate(UpdateType.Fixed).SetSpeedBased(isSpeedBased: true)
					.SetEase(Ease.Linear);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	public void Stop()
	{
		isStopped = true;
		onCapsuleEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		onPlatformEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		StopRumbling();
	}

	public void ReturnPosition()
	{
		reverse = true;
		ParticleSystem[] array = reverseFloorEffects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play(withChildren: true);
		}
		SoundManager.PlayOneShot(reverseEvent);
	}

	private void HandleOverlap()
	{
		if (reverse)
		{
			return;
		}
		Vector3 vector = base.transform.position + (Vector3)Box2D.offset;
		Vector2 size = Box2D.size * base.transform.localScale;
		if (!isStopped)
		{
			Collider2D[] array = AttackUtility.OverlapBoxAll(vector, size, AttackLayer);
			foreach (Collider2D obj in array)
			{
				if (!obj.TryGetComponent<PlayerMover>(out var player))
				{
					continue;
				}
				if ((object)ImahenCutscene != null && ImahenCutscene.IsProducing)
				{
					int currentState = ImahenCutscene.CurrentState;
					if (currentState == 2 || currentState == 4)
					{
						if (!ImahenCutscene.IsResetting)
						{
							isStopped = true;
							player.TryTakeDamage(new HitInfo(this));
							SoundManager.PlayOneShot(hitEvent);
							ImahenCutscene.ResetCutscene(ImahenCutscene.CurrentState).Forget();
							cts.Cancel();
							isRumbling = false;
						}
						continue;
					}
				}
				isStopped = true;
				player.PSM.ChangeState(new PlayerStaggerHitState(player.PSM, delegate
				{
					player.SetGravityFree(active: true);
				}, delegate
				{
					isStopped = false;
					ReturnPosition();
					player.SetGravityFree(active: false);
				})
				{
					Duration = attackDuration,
					NextState = player.PSM.HitState
				});
				PlayerTakeDamage(player).Forget();
			}
		}
		AttackUtility.AttackBox(vector, size, new HitInfo(this, "Enemy"), EnemyAttackLayer);
		AttackCoreMachine(vector, size);
	}

	public void PlaySfx()
	{
		sawInstance.release();
		sawInstance = Game.Instance.SoundManager.PlayOneShotAttached(sawEvent, base.transform);
	}

	public void StopSfx()
	{
		if (sawInstance.isValid())
		{
			sawInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			sawInstance.release();
		}
	}

	public async UniTaskVoid Rumbling()
	{
		isRumbling = true;
		cts = new CancellationTokenSource();
		float sustainTime = impulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_SustainTime;
		SPR.Library.Timer timer = new SPR.Library.Timer(sustainTime);
		while (isRumbling)
		{
			if (timer.IsDone)
			{
				impulseSource.GenerateImpulse();
				timer.Reset();
			}
			timer.Tick(Time.deltaTime);
			await UniTask.Yield(cts.Token);
		}
	}

	public void StopRumbling()
	{
		isRumbling = false;
		cts.Cancel();
	}

	private async UniTaskVoid ChangeSpeedAsync(Vector3 nextPosition, float nextSpeed, bool instant)
	{
		if (!instant)
		{
			await UniTask.WaitWhile(() => base.transform.position.x < nextPosition.x, PlayerLoopTiming.FixedUpdate);
		}
		speed = nextSpeed;
	}

	private async UniTaskVoid PlayerTakeDamage(PlayerMover player)
	{
		int frame = 0;
		FrameTimer cycle = new FrameTimer(attackCycle);
		HitPlayer();
		for (; frame <= attackDuration; frame++)
		{
			if (cycle.IsDone)
			{
				HitPlayer();
				cycle.Reset();
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
			cycle.Tick();
		}
		void HitPlayer()
		{
			player.TryTakeDamage(new HitInfo(this));
			EnableVFX("bullet_time");
			Game.Instance.ObjectPoolManager.GetObject<PoolingParticle>("Player_ChaserDamaged.prefab", player.VisualCenter).Play();
			SoundManager.PlayOneShot(hitEvent);
		}
	}

	private void CheckOverlapCapsule()
	{
		if (reverse)
		{
			onCapsuleEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			return;
		}
		Vector3 point = new Vector3((base.transform.position + new Vector3(Box2D.offset.x + Box2D.size.x / 2f, 0f)).x, capsuleObjectCollider.transform.position.y);
		if (capsuleObjectCollider.bounds.Contains(point) && !onCapsuleEffect.IsAlive())
		{
			onCapsuleEffect.Play();
		}
		else if (!capsuleObjectCollider.bounds.Contains(point) && onCapsuleEffect.IsAlive())
		{
			onCapsuleEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}
	}

	private void CheckOverlapPlatform()
	{
		if (reverse)
		{
			onPlatformEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			return;
		}
		Vector3 vector = base.transform.position + new Vector3(Box2D.offset.x + Box2D.size.x / 2f, 0f);
		BoxCollider2D[] array = platformObjectCollider;
		foreach (BoxCollider2D boxCollider2D in array)
		{
			Vector3 point = new Vector3(vector.x, boxCollider2D.transform.position.y);
			if (boxCollider2D.bounds.Contains(point) && !onPlatformEffect.IsAlive())
			{
				onPlatformEffect.Play();
				return;
			}
			if (boxCollider2D.bounds.Contains(point))
			{
				return;
			}
		}
		if (onPlatformEffect.IsAlive())
		{
			onPlatformEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}
	}

	private void AttackCoreMachine(Vector2 center, Vector2 size)
	{
		Collider2D[] array = AttackUtility.OverlapBoxAll(center, size, EnemyAttackLayer);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].TryGetComponent<ImahenCoreMachine>(out var component))
			{
				if (coreMachineAttackTimer.IsDone)
				{
					component.TryTakeDamage(new HitInfo(this, "CoreMachine"));
					coreMachineAttackTimer.Reset();
				}
				coreMachineAttackTimer.Tick();
				return;
			}
		}
		coreMachineAttackTimer.Reset();
	}
}
