using SPR.Library;
using UnityEngine;

namespace SPR;

public class ImahenExploder : ImahenMachineBase, IAttackSource
{
	[Header("폭발")]
	[SerializeField]
	private int explosionFrameCount;

	[SerializeField]
	private float waveSpeed;

	private FrameTimer timer;

	public override string Name => "Imahen_Exploder";

	public override Direction FacingDirection => Direction.None;

	public override Vector2 VisualCenter => (Vector2)base.transform.position + HitBox.offset;

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		timer = new FrameTimer(explosionFrameCount);
	}

	protected override void Awake()
	{
		base.Awake();
		timer = new FrameTimer(explosionFrameCount);
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (IsTriggered && timer != null)
		{
			if (timer.IsDone)
			{
				Explosion();
				timer = null;
			}
			else
			{
				timer.Tick();
			}
		}
	}

	protected override void OnTriggered()
	{
		base.OnTriggered();
		SetAnimation("all", loop: false);
	}

	protected override void Die()
	{
		base.Die();
		timer = null;
	}

	private void Explosion()
	{
		ObjectPoolManager objectPoolManager = Game.Instance.ObjectPoolManager;
		Vector3 boneWorldPosition = GetBoneWorldPosition("Bomb");
		objectPoolManager.GetObject<MoomooStompWave>("Imahen Exploder Wave.prefab", boneWorldPosition).Emit(waveSpeed).Forget();
		health -= health.Value;
		SetAnimation("break_end_loop");
		if (Enemy.PoolManager.HasPool(address))
		{
			ReturnToPool();
		}
	}
}
