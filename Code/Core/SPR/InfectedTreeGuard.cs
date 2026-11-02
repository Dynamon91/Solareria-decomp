using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;

namespace SPR;

public class InfectedTreeGuard : Creature, IPierce
{
	[Header("분열")]
	[SerializeField]
	private bool isMain;

	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<InfectedTreeGuard> swing;

	[SerializeField]
	private GenericAttackSequence<InfectedTreeGuard> vine;

	[Header("Reference")]
	[SerializeField]
	private ParticleSystem dotParticle;

	[Header("SFX")]
	[SerializeField]
	private EventReference fogEvent;

	private InfectedTreeGuard left;

	private InfectedTreeGuard right;

	public override string ID => Name;

	public override string Name
	{
		get
		{
			if (!isMain)
			{
				return "InfectedTreeGuard_Small";
			}
			return "InfectedTreeGuard";
		}
	}

	public bool IsMain => isMain;

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(swing, () => new List<AttackSequence> { swing });
		AddAttack(vine, () => new List<AttackSequence> { vine });
		if (isMain)
		{
			CreateSmallGuard().Forget();
		}
		else
		{
			base.StateMachine.ChangeState(null);
			Appear().Forget();
		}
		TimelineBeginActions.Add("SpawnVine", delegate
		{
			SpawnVine();
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { swing, vine };
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		return base.GetAttackSequences();
	}

	public void SeparateAsync()
	{
		Separate();
	}

	public void OnEnterPierce()
	{
		dotParticle.Play();
	}

	public void OnExitPierce()
	{
		dotParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
	}

	private async UniTaskVoid CreateSmallGuard()
	{
		left = await AddressableHelper.InstantiateAsync<InfectedTreeGuard>("Infected Tree Guard(Small).prefab", base.transform.parent);
		right = await AddressableHelper.InstantiateAsync<InfectedTreeGuard>("Infected Tree Guard(Small).prefab", base.transform.parent);
		left.gameObject.SetActive(value: false);
		right.gameObject.SetActive(value: false);
		EnemyWave componentInParent = GetComponentInParent<EnemyWave>();
		if ((object)componentInParent != null)
		{
			componentInParent.AddEnemy(left);
			componentInParent.AddEnemy(right);
		}
	}

	private void SpawnVine()
	{
		Enemy.PoolManager.GetObject<InfectedTreeGuardVine>(isMain ? "Infected Tree Guard_Vine.prefab" : "Infected Tree Guard_Vine_Small.prefab", Enemy.Player.GetPositionFromGroud());
	}

	private void Separate()
	{
		float num = HitBox.size.x / 4f;
		left.Flip(-1f);
		left.SetSkin("Infected_Tree_Guard_small_1");
		right.SetSkin("Infected_Tree_Guard_small_2");
		left.Position = VisualBottom + Vector2.left * num;
		right.Position = VisualBottom + Vector2.right * num;
		left.gameObject.SetActive(value: true);
		right.gameObject.SetActive(value: true);
		left.Activate();
		right.Activate();
		left.InitializeOnInstantiate(base.SortingOrder + 1, left.Position);
		right.InitializeOnInstantiate(base.SortingOrder + 2, right.Position);
		left.SetDiableItemDrop(DisableItemDrop);
		right.SetDiableItemDrop(DisableItemDrop);
		ForceStopDeathFire();
	}

	private async UniTaskVoid Appear()
	{
		SetConnectedPath();
		SetAnimation("appear", loop: false);
		await UniTask.WaitUntil(() => IsAnimComplete(0, "appear"));
		base.StateMachine.ChangeState(DefaultState);
	}
}
