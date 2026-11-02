using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerFlameWave : PlayerShockwave
{
	private float knockBackSpeed;

	private int knockBackFrame;

	private readonly List<IDamageable> damageables = new List<IDamageable>();

	public override string Name => "Player_Flame_Wave";

	public override string PrefabKey => "Player Flame Wave.prefab";

	public void Initialize(int minRadius, int maxRadius, int knockBackSpeed, int knockBackFrame)
	{
		SetRadius(minRadius, maxRadius);
		this.knockBackSpeed = knockBackSpeed;
		this.knockBackFrame = knockBackFrame;
		damageables.Clear();
	}

	protected override void OnReturn()
	{
		damageables.Clear();
	}

	protected override void OnContactDamageable(IDamageable damageable)
	{
		Creature creature = damageable as Creature;
		if ((object)creature != null && !damageables.Contains(damageable))
		{
			damageables.Add(damageable);
			bool moveForward = Mathf.Sign(creature.transform.position.x - base.transform.position.x) > 0f == creature.FacingDirection.GetSignX() > 0;
			MoveX(moveForward);
		}
		async void MoveX(bool moveForward2)
		{
			for (int i = 0; i < knockBackFrame; i++)
			{
				creature.MoveX(knockBackSpeed, Time.fixedDeltaTime, moveForward2);
				await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
			}
		}
	}
}
