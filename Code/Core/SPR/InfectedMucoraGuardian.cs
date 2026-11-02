using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class InfectedMucoraGuardian : HighCreature
{
	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<InfectedMucoraGuardian> thunderbolt;

	[SerializeField]
	private GenericBossAttackSequence<InfectedMucoraGuardian> lightning;

	[SerializeField]
	private GenericBossAttackSequence<InfectedMucoraGuardian> combo1;

	[SerializeField]
	private GenericBossAttackSequence<InfectedMucoraGuardian> combo2;

	[SerializeField]
	private GenericBossAttackSequence<InfectedMucoraGuardian> combo3;

	[Header("References")]
	[SerializeField]
	private Transform lightningTransform;

	[SerializeField]
	private EventReference lightningSignEvent;

	[SerializeField]
	private EventReference lightningImpactEvent;

	[SerializeField]
	private int lightningPreDelay;

	private Area area;

	public override Area MyArea => area ?? (area = GetComponentInParent<Area>());

	public override string Name => "InfectedMucoraGuardian";

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		BeginParamActions.Add("Thunderbolt", delegate(int _, Parameter[] param)
		{
			Thunderbolt(param[0].Int, param[1].Int);
		});
		AddAction(ActionTiming.Begin, "Laser", delegate(int duration)
		{
			Laser(duration).Forget();
		});
		BeginParamActions.Add("LaunchProjectile", delegate(int _, Parameter[] param)
		{
			LaunchProjectile(param[0].String, param[1].Float);
		});
		BeginParamActions.Add("LaunchSphere", delegate(int _, Parameter[] param)
		{
			LaunchSphere(param[0].Int, param[1].Float, param[2].Float, param[3].Int, param[4].Int, param[5].Int, param[6].Int).Forget();
		});
		AttackPatterns.TryAdd(thunderbolt, () => new List<AttackSequence> { thunderbolt });
		AttackPatterns.TryAdd(lightning, () => new List<AttackSequence> { lightning });
		AttackPatterns.TryAdd(combo1, () => new List<AttackSequence>
		{
			combo1,
			CryptoRandom.RandomBool() ? thunderbolt : lightning
		});
		AttackPatterns.TryAdd(combo2, () => new List<AttackSequence> { combo2 });
		AttackPatterns.TryAdd(combo3, () => new List<AttackSequence> { combo3 });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { thunderbolt, lightning, combo1, combo2, combo3 };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return base.GenerateCurrentAttackSequences();
	}

	protected override void OnDie()
	{
		base.OnDie();
		LegendaryArea legendaryArea = MyArea.CastObject<LegendaryArea>();
		DamageEffectAsync().Forget();
		legendaryArea.OpenWall();
		legendaryArea.PlayClearTimeline(VisualCenter).Forget();
	}

	protected override List<AttackSequence> GetRandomSequence()
	{
		List<int> list = new List<int>();
		if (IsPlayerInAttackBound(thunderbolt))
		{
			list.AddRange(Enumerable.Repeat(0, thunderbolt.GetPower(this)));
		}
		if (IsPlayerInAttackBound(lightning))
		{
			list.AddRange(Enumerable.Repeat(1, lightning.GetPower(this)));
		}
		if (IsPlayerInAttackBound(combo1))
		{
			list.AddRange(Enumerable.Repeat(2, combo1.GetPower(this)));
		}
		if (IsPlayerInAttackBound(combo2))
		{
			list.AddRange(Enumerable.Repeat(3, combo2.GetPower(this)));
		}
		if (IsPlayerInAttackBound(combo3))
		{
			list.AddRange(Enumerable.Repeat(4, combo3.GetPower(this)));
		}
		int num = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list2 = new List<BossAttackSequence> { thunderbolt, lightning, combo1, combo2, combo3 };
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num)
			{
				list2[i].RisePower();
			}
		}
		return list[CryptoRandom.Range(0, list.Count)] switch
		{
			0 => new List<AttackSequence> { thunderbolt }, 
			1 => new List<AttackSequence> { lightning }, 
			2 => new List<AttackSequence> { combo1 }, 
			3 => new List<AttackSequence> { combo2 }, 
			4 => new List<AttackSequence> { combo3 }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private void Thunderbolt(int signDuration, int duration)
	{
		LightningAttack thunderboltAttack = Enemy.PoolManager.GetObject<LightningAttack>("Infected Mucora Guardian_Thunderbolt.prefab", Enemy.Player.GetPositionFromGroud());
		thunderboltAttack.Activate(signDuration, duration).Forget();
		Enemy.SoundManager.PlayOneShotAttached(lightningSignEvent, thunderboltAttack.transform);
		UniTaskExtension.WaitForFixedUpdatesOrCancel(signDuration - lightningPreDelay).GetAwaiter().OnCompleted(delegate
		{
			Enemy.SoundManager.PlayOneShotAttached(lightningImpactEvent, thunderboltAttack.transform);
		});
	}

	private async UniTaskVoid Laser(int duration)
	{
		Vector2 vector = ((FacingDirection.GetSignX() < 0) ? MyArea.AreaBox.UpperRight : MyArea.AreaBox.LowerLeft) + -FacingDirection.ToVector2() * MyArea.AreaBox.Size.x / 6f;
		vector.y = VisualCenter.y;
		InfectedMucoraGuardianLaser laser = Enemy.PoolManager.GetObject<InfectedMucoraGuardianLaser>("Infected Mucora Guardian_Laser.prefab", vector, lightningTransform);
		laser.transform.rotation = Quaternion.Euler(0f, 0f, (float)(-FacingDirection.GetSignX()) * 90f);
		laser.Play();
		for (int frame = 0; frame < duration; frame++)
		{
			laser.HandleOverlap();
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
	}

	private void LaunchProjectile(string projectileAddress, float speed)
	{
		Enemy.PoolManager.GetObject<Projectile>(projectileAddress, VisualCenter).Launch(FacingDirection.ToVector2(), speed);
	}

	private async UniTaskVoid LaunchSphere(int count, float gap, float speed, int dropWaitFrame, int delayFrame, int cycleFrame, int duration)
	{
		Vector3 vector = new Vector3(MyArea.AreaBox.Center.x, MyArea.AreaBox.UpperRight.y);
		InfectedMucoraGuardianSphere[] spheres = new InfectedMucoraGuardianSphere[count];
		bool flag = count % 2 != 0;
		for (int i = ((!flag) ? 1 : 0); i < (flag ? count : (count + 1)); i++)
		{
			int num = Mathf.CeilToInt((float)i / 2f);
			Vector3 vector2 = (flag ? new Vector3((float)num * gap, 0f) : new Vector3((float)(num - 1) * gap + gap / 2f, 0f));
			Vector3 pos = vector + ((i % 2 == 0) ? vector2 : (-vector2));
			InfectedMucoraGuardianSphere infectedMucoraGuardianSphere = Enemy.PoolManager.GetObject<InfectedMucoraGuardianSphere>("Infected Mucora Guardian_Sphere.prefab", pos);
			infectedMucoraGuardianSphere.PlayEffect();
			spheres[flag ? i : (i - 1)] = infectedMucoraGuardianSphere;
		}
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(dropWaitFrame);
		InfectedMucoraGuardianSphere[] array = spheres;
		foreach (InfectedMucoraGuardianSphere obj in array)
		{
			obj.SetCycle(delayFrame, cycleFrame, duration);
			obj.Launch(Vector2.down, speed);
		}
	}
}
