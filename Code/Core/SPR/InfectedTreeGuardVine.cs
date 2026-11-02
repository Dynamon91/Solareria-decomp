using System.Collections.Generic;
using FSM;
using Library;
using SPR.FSM;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class InfectedTreeGuardVine : Enemy, IPoolableObject
{
	public const string AddressBig = "Infected Tree Guard_Vine.prefab";

	public const string AddressSmall = "Infected Tree Guard_Vine_Small.prefab";

	[SerializeField]
	private string address;

	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<InfectedTreeGuardVine> rise1;

	[SerializeField]
	private GenericAttackSequence<InfectedTreeGuardVine> rise2;

	public string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	public override Groggy Groggy => null;

	public override string Name
	{
		get
		{
			if (!address.Equals("Infected Tree Guard_Vine.prefab"))
			{
				return "InfectedTreeGuard_Small_Vine";
			}
			return "InfectedTreeGuard_Vine";
		}
	}

	public override EnemyState DefaultState => null;

	public override HitType HitType => HitType.None;

	protected override void Awake()
	{
		base.StateMachine = new SimpleStateMachine();
		base.VfxController.InitializeVFXDictionary();
	}

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
		base.StateMachine.ChangeState(new InfectedTreeGuardVineState(this, CryptoRandom.RandomBool() ? rise1 : rise2));
	}

	public void ReturnToPool()
	{
		RemoveAnimation();
		Game.Instance.ObjectPoolManager.Return(this);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { rise1, rise2 };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence>();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		return false;
	}
}
