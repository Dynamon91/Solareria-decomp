using System.Collections.Generic;
using Library;
using SPR.FSM;
using SPR.Field;
using SPR.ObjectPool;
using Spine;
using UnityEngine;

namespace SPR;

public class TycheClone : Enemy, IPoolableObject
{
	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<TycheClone> cloneButt;

	[SerializeField]
	private GenericAttackSequence<TycheClone> darkBreath;

	[SerializeField]
	private GenericAttackSequence<TycheClone> compressionBreath;

	[Header("Reference")]
	[SerializeField]
	private Transform swapEffectTransform;

	private Tyche tyche;

	private bool buttHit;

	public override string Name => "TycheClone";

	public override EnemyState DefaultState => null;

	public override HitType HitType => HitType.Platform;

	public string PrefabKey => "TycheClone.prefab";

	public GameObject GameObject => base.gameObject;

	public Area Area { get; set; }

	public Transform SwapEffectTransform => swapEffectTransform;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(this);
		ResetStatus();
	}

	public void ReturnToPool()
	{
		DisableVFXAll();
		base.StateMachine.ChangeState(null);
		RemoveAnimation();
		base.SkeletonAnimation.Skeleton.SetToSetupPose();
		Enemy.PoolManager.Return(this);
	}

	protected override void Awake()
	{
		base.Awake();
		AddAction(ActionTiming.Begin, "ShootAirCannon", delegate(Parameter[] p)
		{
			ShootAirCannon(p[0].Float, p[1].Float, p[2].Int, p[3].Int);
		});
		AddAnimationEvent(AnimState.Complete, OnCompleteCompressionBreath);
	}

	protected override void OnDestroy()
	{
		RemoveAnimationEvent(AnimState.Complete, OnCompleteCompressionBreath);
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		if (!buttHit)
		{
			AreaBox areaBox = Area.AreaBox;
			float num = HitBox.size.x / 2f;
			float num2 = VisualCenter.x - num;
			float num3 = VisualCenter.x + num;
			Vector2 vector = (moveForward ? FacingDirection.ToVector2() : FacingDirection.Opposite().ToVector2());
			float num4 = vector.x * velocityX * deltaTime;
			if ((vector.x < 0f && (float)areaBox.LowerLeft.x < num2 + num4) || (vector.x > 0f && (float)areaBox.UpperRight.x > num3 + num4))
			{
				base.transform.position += (Vector3)vector * (velocityX * deltaTime);
			}
		}
	}

	public void SetMainBody(Tyche mainBody)
	{
		tyche = mainBody;
	}

	public void Butt()
	{
		buttHit = false;
		LookPlayer();
		base.StateMachine.ChangeState(new TycheCloneAttackState(this, () => new List<AttackSequence> { cloneButt }));
	}

	public void SetCompressionBreath()
	{
		base.StateMachine.ChangeState(new TycheCloneAttackState(this, () => new List<AttackSequence> { compressionBreath }));
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("Butt"))
		{
			buttHit = true;
		}
	}

	public override void OnPlatformHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("Butt"))
		{
			buttHit = true;
		}
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence>();
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence>();
	}

	private void ShootAirCannon(float speed, float heightPower, int cycle, int duration)
	{
		Vector2 vector = GetBoneWorldPosition("mouth");
		Enemy.PoolManager.GetObject<TycheAirCannon>("TycheAirCannon.prefab", vector).Shoot(speed, heightPower, new Vector2(Enemy.Player.VisualBottom.x, tyche.SpawnPosition.y), cycle, duration);
	}

	private void OnCompleteCompressionBreath(TrackEntry track)
	{
		if (track.Animation.Name == "clone_compression_breath")
		{
			SetAnimation("idle");
		}
	}
}
