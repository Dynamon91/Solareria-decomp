using DG.Tweening;
using FMODUnity;
using UnityEngine;

namespace SPR;

public class TycheAirCannon : Projectile
{
	[SerializeField]
	private EventReference hitEvent;

	private int fogAttackCycle;

	private int fogDuration;

	public override string Name => "TycheAirCannon";

	public override string PrefabKey => "TycheAirCannon.prefab";

	public override void ReturnToPool()
	{
		DOTween.Kill(base.transform);
		Game.Instance.ObjectPoolManager.GetObject<TycheBreathFog>("TycheCompressionBreathFog.prefab", base.transform.position).Activate(fogAttackCycle, fogDuration).Forget();
		Game.Instance.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		base.ReturnToPool();
	}

	protected override void OnParry()
	{
		DOTween.Kill(base.transform);
	}

	protected override void OnHit()
	{
		if (Projectile.Player.PSM.CurrentState == Projectile.Player.PSM.GuardState)
		{
			ReturnToPool();
		}
	}

	public void Shoot(float speed, float height, Vector2 destination, int cycle, int duration)
	{
		Speed = speed;
		MoveDirection = (destination - (Vector2)base.transform.position).normalized;
		Vector3 vector = base.transform.position + new Vector3(0f, height);
		float num = Vector2.Distance(base.transform.position, vector) + Vector2.Distance(vector, destination);
		base.transform.DOJump(destination, height, 1, num / speed).SetEase(Ease.Linear).OnUpdate(delegate
		{
			HandleOverlap(base.transform.position, base.OverlapSize);
		});
		fogAttackCycle = cycle;
		fogDuration = duration;
	}
}
