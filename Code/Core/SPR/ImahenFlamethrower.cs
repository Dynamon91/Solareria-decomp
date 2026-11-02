using System;
using System.Text;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class ImahenFlamethrower : ImahenMachineBase, IAttackSource
{
	[Flags]
	private enum FlameDirection
	{
		None = 0,
		Top = 1,
		Left = 2,
		Right = 4
	}

	[SerializeField]
	private FlameDirection direction;

	[SerializeField]
	private int attackCycleFrame;

	[SerializeField]
	private Zone[] attackZone;

	[SerializeField]
	private Transform top;

	[SerializeField]
	private Transform left;

	[SerializeField]
	private Transform right;

	private FrameTimer timer;

	public override string Name => "Imahen_Flamethrower";

	public override Direction FacingDirection => Direction.None;

	protected override void Awake()
	{
		base.Awake();
		timer = new FrameTimer(attackCycleFrame);
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (base.IsOperate)
		{
			if (timer.PassedFrame == attackCycleFrame - 40)
			{
				SetAnimationInstantly("spray/" + GetSuffix(), loop: false);
			}
			if (timer.IsDone)
			{
				Attack();
				timer.Reset();
			}
			timer.Tick();
		}
	}

	protected override void OnTriggered()
	{
		base.OnTriggered();
		SetAnimation("operate/" + GetSuffix(), loop: false);
	}

	protected override void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		if (direction.HasFlag(FlameDirection.Top))
		{
			Zone zoneOf = attackZone[0].GetZoneOf(top);
			Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
		}
		if (direction.HasFlag(FlameDirection.Left))
		{
			Zone zoneOf2 = attackZone[1].GetZoneOf(left);
			Gizmos.DrawWireCube(zoneOf2.Center, zoneOf2.Size);
		}
		if (direction.HasFlag(FlameDirection.Right))
		{
			Zone zoneOf3 = attackZone[2].GetZoneOf(right);
			Gizmos.DrawWireCube(zoneOf3.Center, zoneOf3.Size);
		}
	}

	private void Attack()
	{
		ObjectPoolManager objectPoolManager = Game.Instance.ObjectPoolManager;
		if (direction.HasFlag(FlameDirection.Top))
		{
			objectPoolManager.GetObject<PoolingParticle>("Imahen Flamethrower Effect.prefab", top.position).Play();
			Zone zoneOf = attackZone[0].GetZoneOf(base.transform);
			AttackUtility.AttackBox(zoneOf.Center, zoneOf.Size, new HitInfo(this), LayerUtility.PlayerLayer);
		}
		if (direction.HasFlag(FlameDirection.Left))
		{
			PoolingParticle poolingParticle = objectPoolManager.GetObject<PoolingParticle>("Imahen Flamethrower Effect.prefab", left.position);
			poolingParticle.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
			poolingParticle.Play();
			Zone zoneOf2 = attackZone[1].GetZoneOf(base.transform);
			AttackUtility.AttackBox(zoneOf2.Center, zoneOf2.Size, new HitInfo(this), LayerUtility.PlayerLayer);
		}
		if (direction.HasFlag(FlameDirection.Right))
		{
			PoolingParticle poolingParticle2 = objectPoolManager.GetObject<PoolingParticle>("Imahen Flamethrower Effect.prefab", right.position);
			poolingParticle2.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
			poolingParticle2.Play();
			Zone zoneOf3 = attackZone[2].GetZoneOf(base.transform);
			AttackUtility.AttackBox(zoneOf3.Center, zoneOf3.Size, new HitInfo(this), LayerUtility.PlayerLayer);
		}
	}

	private string GetSuffix()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (direction.HasFlag(FlameDirection.Left))
		{
			stringBuilder.Append("left");
		}
		if (direction.HasFlag(FlameDirection.Right))
		{
			stringBuilder.Append((stringBuilder.Length != 0) ? "_right" : "right");
		}
		if (direction.HasFlag(FlameDirection.Top))
		{
			stringBuilder.Append((stringBuilder.Length != 0) ? "_top" : "top");
		}
		return stringBuilder.ToString();
	}
}
