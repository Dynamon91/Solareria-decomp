using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.FSM;
using SPR.Field;
using SPR.ObjectPool;
using SPR.UI;
using Spine;
using UnityEngine;

namespace SPR;

public abstract class ImahenMachineBase : Enemy, IPoolableObject
{
	[Header("Address")]
	[SerializeField]
	protected string address;

	[Header("Death")]
	[SerializeField]
	private int deathFadeDuration = 60;

	[Header("References")]
	[SerializeField]
	protected BoxCollider2D trigger;

	protected DG.Tweening.Sequence VisualKnockBackSequence;

	protected CreatureCanvas CreatureCanvas;

	protected ImahenSection ImahenSection;

	private CancellationTokenSource animationPauseCts;

	private ImahenArea area;

	public string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	public override string Name => "";

	public override HitType HitType => HitType.Object;

	public override Vector2 VisualCenter => (Vector2)base.transform.position + (Vector2)(base.transform.rotation * new Vector2(visualCenterPivot.x * (float)FacingDirection.GetSignX(), visualCenterPivot.y));

	public override EnemyState DefaultState => null;

	protected virtual bool IsTriggered => !trigger.gameObject.activeSelf;

	protected bool IsOperate { get; private set; }

	protected override float VisualKnockBackValue => 1f;

	protected override float VisualKnockBackDuration => 1f / 15f;

	protected virtual string DeathAnimation => "break";

	protected ImahenArea Area => area ?? (area = GetComponentInParent<ImahenArea>());

	public virtual void InitializeBeforeUse()
	{
		base.SkeletonRenderer.Skeleton.SetAlpha(1f);
		base.gameObject.SetActive(value: true);
		if (animationPauseCts == null)
		{
			animationPauseCts = new CancellationTokenSource();
		}
		ResetStatus();
		SetAnimationInstantly("standby");
		OnTriggered();
		base.Money = 0;
		base.Exp = 0;
	}

	public virtual void ReturnToPool()
	{
		if ((object)ImahenSection != null)
		{
			LazySingleton<MessageSystem>.Instance.Publish(new ImahenMachineDestroyEvent(ImahenSection));
			ForceStopDeathFire();
			Game.Instance.ObjectPoolManager.Return(this);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		HitBox.enabled = false;
		SetAnimation("standby");
		if (animationPauseCts == null)
		{
			animationPauseCts = new CancellationTokenSource();
		}
		AddAnimationEvent(AnimState.Complete, OnAnimationComplete);
	}

	protected override void OnFixedUpdate()
	{
		if (trigger.gameObject.activeSelf && trigger.bounds.Intersects(Game.Instance.Player.mover.ColliderBounds))
		{
			OnTriggered();
		}
	}

	public void SetSection(ImahenSection section)
	{
		ImahenSection = section;
	}

	protected override void OnChangeHealth(int delta)
	{
		CreatureCanvas.UpdateHealthBar(health.Value, maxHealth, delta);
		CreatureCanvas.UpdateStunPoint(base.CurrentStun);
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		base.OnDamaged(hitInfo);
		VisualKnockBack(hitInfo.GetDirection(VisualBottom));
	}

	protected override void Die()
	{
		base.Die();
		IsOperate = false;
		HitBox.enabled = false;
		animationPauseCts.Cancel();
		base.SkeletonAnimation.timeScale = 1f;
		base.StateMachine.ChangeState(null);
		SetAnimation(DeathAnimation, loop: false);
		Object.Destroy(CreatureCanvas.gameObject);
		CreatureCanvas = null;
	}

	public override void VisualKnockBack(Direction direction)
	{
		VisualKnockBackSequence?.Kill();
		VisualKnockBackSequence = DOTween.Sequence();
		Transform tr = base.SkeletonAnimation.transform;
		VisualKnockBackSequence.Append(tr.DOLocalMoveX(VisualKnockBackValue * (float)FacingDirection.GetSignX() * direction.ToVector2().x, VisualKnockBackDuration).SetEase(Ease.Linear)).Append(tr.DOLocalMoveX(0f, VisualKnockBackDuration).SetEase(Ease.Linear)).OnKill(delegate
		{
			tr.localPosition = Vector3.zero;
		});
		PauseAndJumpAnimation(3, TaskUtil.RefreshToken(ref animationPauseCts)).Forget();
	}

	protected virtual void OnTriggered()
	{
		CreateCreatureCanvas().Forget();
		trigger.gameObject.SetActive(value: false);
		IsOperate = true;
		HitBox.enabled = true;
	}

	protected async UniTaskVoid CreateCreatureCanvas()
	{
		CreatureCanvas = await AddressableHelper.InstantiateAsync<CreatureCanvas>("Creature Canvas.prefab");
		CreatureCanvas.Initialize(VisualCenter, Bounds.size.y / 2f, base.MaxStunCount);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence>();
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence>();
	}

	protected async UniTaskVoid ReturnToPoolAsync()
	{
		await base.SkeletonRenderer.Skeleton.DOAlpha(0f, Util.FrameToSeconds(deathFadeDuration)).WaitAsync();
		ReturnToPool();
	}

	protected virtual void OnAnimationComplete(TrackEntry trackEntry)
	{
		if (trackEntry.Animation.Name == DeathAnimation && Enemy.PoolManager.HasPool(address))
		{
			ReturnToPoolAsync().Forget();
		}
	}
}
