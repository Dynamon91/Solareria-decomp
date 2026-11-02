using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class WindSpiritFighterClone : SpineObject, IPoolableObject
{
	public override string Name => "WindSpiritFighterClone";

	public string PrefabKey => "Wind Spirit Fighter Clone.prefab";

	public GameObject GameObject => base.gameObject;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
