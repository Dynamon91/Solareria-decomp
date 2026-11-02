using Cysharp.Threading.Tasks;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class SoowooFragment : Projectile
{
	[Header("데미지 판정")]
	[SerializeField]
	private int noDamageFrame;

	[Header("Reference")]
	[SerializeField]
	private ParticleSystem impactParticle;

	private FrameTimer timer;

	public override string Name => "SoowooFragment";

	public override string PrefabKey => "Soowoo Fragment.prefab";

	public void Initialize(Vector2 direction, float power, float gravity)
	{
		MoveDirection = direction;
		Speed = power;
		Gravity = gravity;
		SetVelocity();
		if (timer == null)
		{
			timer = new FrameTimer(noDamageFrame);
		}
		timer.Reset();
		AttackAvailable = false;
	}

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		base.SkeletonAnimation.gameObject.SetActive(value: true);
	}

	public override void ReturnToPool()
	{
		Impact().Forget();
	}

	protected override void Move()
	{
		base.Move();
		if (timer.IsDone)
		{
			AttackAvailable = true;
		}
		timer.Tick();
	}

	private async UniTaskVoid Impact()
	{
		Speed = 0f;
		base.SkeletonAnimation.gameObject.SetActive(value: false);
		impactParticle.Play();
		await UniTask.WaitWhile(() => impactParticle.isPlaying);
		base.ReturnToPool();
	}
}
