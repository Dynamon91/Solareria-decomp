using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class SeedKeeperVine : SpineObject, IPoolableObject
{
	[SerializeField]
	private string prefabKey;

	public override string Name => "SeedKeeper_Vine";

	public string PrefabKey => prefabKey;

	public GameObject GameObject => base.gameObject;

	public int Duration => base.VfxController.GetDuration("drain");

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		SetAnimation("hide");
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
