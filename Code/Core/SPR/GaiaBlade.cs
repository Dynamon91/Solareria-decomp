using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dreamteck.Splines;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class GaiaBlade : PoolingObject, IAttackSource
{
	public const string Address = "Gaia_Blade.prefab";

	private BoxCollider2D box2D;

	private SplineFollower splineFollower;

	private List<IDamageable> damageList;

	private PlayerMover player;

	public string Name => "GaiaBlade";

	public Direction FacingDirection
	{
		get
		{
			if ((Player.transform.position - Gaia.transform.position).normalized.x.GetSign() != -1)
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
	}

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private SplineFollower SplineFollower => splineFollower ?? (splineFollower = GetComponent<SplineFollower>());

	private PlayerMover Player => player ?? (player = Game.Instance.Player.mover);

	public Gaia Gaia { get; set; }

	public bool IsAttack { get; set; } = true;

	public Vector2 OverlapSize { get; set; }

	public event Action OnFlying;

	public override void InitializeBeforeUse()
	{
		ResetDamageCache();
		damageList = new List<IDamageable>();
		base.InitializeBeforeUse();
	}

	public override void ReturnToPool()
	{
		SplineFollower.spline = null;
		this.OnFlying = null;
		base.ReturnToPool();
	}

	public void SetAlpha(float value)
	{
	}

	public void Rotate(Vector2 target)
	{
		Vector2 normalized = (target - (Vector2)base.transform.position).normalized;
		base.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, normalized));
	}

	public void SetSpline(SplineComputer spline)
	{
		SplineFollower.spline = spline;
		SplineFollower.Restart();
		Vector3 normalized = (spline[spline.pointCount - 1].position - spline[0].position).normalized;
		base.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, normalized));
	}

	public Tween Move(int duration, bool useRotationOffset = true, bool needReset = true)
	{
		if (useRotationOffset)
		{
			SplineFollower.motion.applyRotation = true;
			SplineFollower.motion.rotationOffset = new Vector3(0f, 0f, -90f);
		}
		else
		{
			SplineFollower.motion.applyRotation = false;
			SplineFollower.motion.rotationOffset = Vector3.zero;
		}
		return SplineFollower.DOFollow(Util.FrameToSeconds(duration), needReset);
	}

	public void HandleOverlap()
	{
		foreach (IDamageable item in AttackUtility.AttackBox(hitInfo: new HitInfo(this), center: base.transform.position, size: Box2D.size * base.transform.localScale, attackLayer: LayerUtility.PlayerLayer, attackedList: damageList))
		{
			if (Gaia.CanMarkPlayer(item))
			{
				Gaia.MarkToPlayer().Forget();
			}
		}
	}

	public void HandleOverlap(List<IDamageable> damageableList, float delta = 1f)
	{
		if (!IsAttack)
		{
			return;
		}
		foreach (IDamageable item in AttackUtility.AttackBox(hitInfo: new HitInfo(this), center: base.transform.position, size: new Vector2(OverlapSize.x * base.transform.localScale.x, OverlapSize.y * base.transform.localScale.y * delta), angle: base.transform.rotation.eulerAngles.z, attackLayer: LayerUtility.PlayerLayer, attackedList: damageableList))
		{
			if (Gaia.CanMarkPlayer(item))
			{
				Gaia.MarkToPlayer().Forget();
			}
		}
	}

	public async UniTaskVoid Launch(Vector2 direction, float speed, AreaBox area, List<IDamageable> list)
	{
		OverlapSize = Box2D.size;
		while (area.IsInArea(base.transform.position, 4f))
		{
			this.OnFlying?.Invoke();
			HandleOverlap(list, speed * Time.fixedDeltaTime);
			base.transform.position += (Vector3)direction * (speed * Time.fixedDeltaTime);
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		ReturnToPool();
	}

	private void ResetDamageCache()
	{
		damageList = null;
	}
}
