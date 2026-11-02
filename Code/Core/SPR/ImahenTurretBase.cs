using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.FSM;
using SPR.Library;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public abstract class ImahenTurretBase : ImahenMachineBase
{
	[SerializeField]
	private float rotationSpeed;

	[SerializeField]
	protected Zone patrolSightArea;

	[SerializeField]
	private int alertMinFrame;

	[SerializeField]
	private int alertMaxFrame;

	[Header("Electric Field")]
	[SerializeField]
	private int fieldCycle;

	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<ImahenTurretBase> attack;

	protected Vector2 AimDirection;

	private ImahenTurretStandbyState standbyState;

	private CancellationTokenSource animationPauseCts;

	private bool isPooled;

	private Vector2 spawnPosition;

	public override string Name => "Imahen_Turret";

	public override EnemyState DefaultState => standbyState;

	public bool InOperation { get; private set; }

	public ImahenTurretIdleState IdleState { get; private set; }

	public ImahenTurretAttackState TurretAttackState { get; private set; }

	protected override string DeathAnimation => "death";

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		InOperation = true;
		isPooled = true;
	}

	public override void ReturnToPool()
	{
		InOperation = false;
		isPooled = false;
		base.ReturnToPool();
	}

	protected override void Awake()
	{
		base.Awake();
		standbyState = new ImahenTurretStandbyState(this);
		IdleState = new ImahenTurretIdleState(this, alertMinFrame, alertMaxFrame);
		TurretAttackState = new ImahenTurretAttackState(this, () => new List<AttackSequence> { attack });
		base.StateMachine.ChangeState(DefaultState);
		AddAction(ActionTiming.Begin, "Fire", delegate(Parameter[] p)
		{
			Fire(p[0].Float);
		});
		AddAction(ActionTiming.OnFrame, "Aim", Aim);
		HitBox.enabled = false;
	}

	private void Start()
	{
		spawnPosition = base.transform.position.ToVector2Int();
		bool active = !Game.Instance.RuntimeData.IsAlreadyDieCreature(Game.Instance.CurrentFieldID, spawnPosition);
		base.gameObject.SetActive(active);
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (!(hitInfo.Source is Projectile { Reversed: false }))
		{
			return base.TryTakeDamage(hitInfo);
		}
		return false;
	}

	protected override void Die()
	{
		base.Die();
		if (!isPooled)
		{
			Game.Instance.RuntimeData.AddDieCreature(Game.Instance.CurrentFieldID, spawnPosition);
		}
	}

	public virtual bool IsPlayerInSightBound()
	{
		Zone zoneOf = patrolSightArea.GetZoneOf(this);
		if (!IsPlayerInBound(zoneOf.Center, zoneOf.Size))
		{
			return false;
		}
		Vector2 normalized = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		float distance = Mathf.Min((Enemy.Player.VisualCenter - VisualCenter).magnitude, patrolSightArea.Size.x / 2f);
		return (object)Physics2D.Raycast(VisualCenter, normalized, distance, LayerUtility.PlatformLayer).collider == null;
	}

	public void Aim()
	{
		Bone bone = base.SkeletonRenderer.Skeleton.FindBone("Target");
		Vector2 vector = (Enemy.Player.VisualCenter + new Vector2(0f, Enemy.Player.ColliderSize.y / 4f) - (Vector2)GetBoneWorldPosition("Center")).normalized * 10f;
		Vector2 skeletonSpacePosition = Vector2.MoveTowards(target: base.transform.InverseTransformPoint((Vector2)GetBoneWorldPosition("Center") + vector), current: bone.GetSkeletonSpacePosition(), maxDistanceDelta: rotationSpeed * Time.fixedDeltaTime);
		bone.SetPositionSkeletonSpace(skeletonSpacePosition);
		AimDirection = (GetBoneWorldPosition("Target") - GetBoneWorldPosition("Fire")).normalized;
	}

	public async UniTaskVoid Operate()
	{
		if (!InOperation)
		{
			CreateCreatureCanvas().Forget();
			SetAnimation("operate", loop: false);
			HitBox.enabled = true;
			await UniTask.WaitUntil(() => IsAnimComplete(0, "operate"));
			base.StateMachine.ChangeState(IdleState);
		}
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	protected abstract void Fire(float speed);

	protected override async UniTask DamageEffectAsync()
	{
		PauseAndJumpAnimation(3, TaskUtil.RefreshToken(ref animationPauseCts)).Forget();
		await UniTask.CompletedTask;
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
	}

	protected override void OnTriggered()
	{
		base.OnTriggered();
		Operate().Forget();
		InOperation = true;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.yellow;
		Zone zoneOf = patrolSightArea.GetZoneOf(this);
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
	}
}
