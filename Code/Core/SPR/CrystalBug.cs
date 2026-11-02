using System.Collections.Generic;
using DG.Tweening;
using FMODUnity;
using Library;
using UnityEngine;

namespace SPR;

public class CrystalBug : Creature
{
	[SerializeField]
	private int spawnStalactiteChance;

	[SerializeField]
	private float fallingSeconds;

	[SerializeField]
	private ParticleSystem hitParticle;

	[SerializeField]
	private EventReference playerHitEvent;

	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<CrystalBug> headbutt;

	public override string Name => "CrystalBug";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.BounceAnim = "atk_parry";
		AttackPatterns.TryAdd(headbutt, () => new List<AttackSequence> { headbutt });
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (CryptoRandom.Range(1, 101) < spawnStalactiteChance)
		{
			Stalactite stalactite = Enemy.PoolManager.GetObject<Stalactite>("Stalactite.prefab", VisualCenter);
			ShortcutExtensions.DOJump(endValue: new Vector2(VisualBottom.x + Random.Range(-2f, 2f), VisualBottom.y), target: stalactite.transform, jumpPower: 2f, numJumps: 1, duration: fallingSeconds).SetEase(Ease.Linear).OnComplete(delegate
			{
				stalactite.Explode().Forget();
			});
			stalactite.SortingOrder = Enemy.PoolManager.GetActiveObjectCount("Stalactite.prefab") + 1;
		}
		return base.TryTakeDamage(hitInfo);
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (!Enemy.Player.PSM.StateOption.HasFlag(StateOption.Dodge) && !Enemy.Player.CanParry(hitInfo))
		{
			hitParticle.Play();
			Game.Instance.SoundManager.PlayOneShotAttached(playerHitEvent, base.transform);
		}
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { headbutt };
	}
}
