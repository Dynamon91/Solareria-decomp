using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class InfectedWarriorFragment : Projectile
{
	[Header("데미지 판정")]
	[SerializeField]
	private int noDamageFrame;

	private FrameTimer timer;

	public override string Name => "InfectedWarriorFragment";

	public override string PrefabKey => "Infected Warrior Fragment.prefab";

	public void Initialize(Vector2 direction, float power, float gravity, bool kick)
	{
		SetSkin(kick ? "Fragment_1" : $"Fragment_{CryptoRandom.Range(1, 6)}");
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

	protected override void Move()
	{
		base.Move();
		if (timer.IsDone)
		{
			AttackAvailable = true;
		}
		timer.Tick();
	}
}
