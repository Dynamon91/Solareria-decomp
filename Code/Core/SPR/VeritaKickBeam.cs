using System.Collections.Generic;
using Library;
using SPR.Library;
using UnityEngine;
using UnityEngine.Serialization;

namespace SPR;

public class VeritaKickBeam : PoolingObject, IAttackSource
{
	[SerializeField]
	private string attackName;

	[FormerlySerializedAs("cycle")]
	[SerializeField]
	private int flameFieldCycle = 30;

	[FormerlySerializedAs("duration")]
	[SerializeField]
	private int flameFieldDuration = 180;

	private float width;

	private Vector3 end;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private FrameTimer timer;

	public string Name => attackName;

	public Direction FacingDirection
	{
		get
		{
			if (!(base.transform.localScale.x > 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		damageableList.Clear();
		if (timer == null)
		{
			timer = new FrameTimer(10);
		}
	}

	public override void ReturnToPool()
	{
		width = 0f;
		base.ReturnToPool();
	}

	private void FixedUpdate()
	{
		if (timer.IsDone)
		{
			Move();
			timer.Reset();
		}
		timer.Tick();
	}

	public void Initialize(float speed, Vector3 end)
	{
		int sign = base.transform.localScale.x.GetSign();
		end.x += (float)sign * speed * Time.fixedDeltaTime;
		width = speed * Time.fixedDeltaTime;
		this.end = end;
	}

	protected virtual void Move()
	{
		Game.Instance.ObjectPoolManager.GetObject<GatekeeperFlameField>("Verita Flame Field.prefab", base.transform.position + new Vector3(0f, -4f)).Initialize(flameFieldDuration, flameFieldCycle, width, setParticle: false);
		if (!(width <= 0f))
		{
			if (base.transform.position == end)
			{
				ReturnToPool();
			}
			else
			{
				base.transform.position = Vector3.MoveTowards(base.transform.position, end, width);
			}
		}
	}
}
