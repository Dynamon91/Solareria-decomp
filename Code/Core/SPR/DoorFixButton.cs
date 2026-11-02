using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class DoorFixButton : MonoBehaviour, IDamageable, IPoolableObject
{
	private Lever lever;

	public string PrefabKey => "TrapDoorFixButton.prefab";

	public GameObject GameObject => base.gameObject;

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter => base.transform.position;

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(Lever lever)
	{
		this.lever = lever;
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		lever.TryFixDoorOpen();
		return true;
	}
}
