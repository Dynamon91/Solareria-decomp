using Cysharp.Threading.Tasks;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class CoralCrabGas : MonoBehaviour, IPoolableObject, IAttackSource
{
	public string PrefabKey => "Coral Crab Gas.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "CoralCrabGas";

	public Direction FacingDirection => Direction.Right;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(int duration, int interval)
	{
		HandleOverlap(duration, interval).Forget();
	}

	private async UniTaskVoid HandleOverlap(int duration, int interval)
	{
		for (int frame = 0; frame <= duration; frame++)
		{
			if (frame % interval == 0)
			{
				AttackUtility.AttackBox(base.transform.position, base.transform.localScale, new HitInfo(this, string.Empty), LayerUtility.EnemyDefaultAttackLayer);
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		ReturnToPool();
	}
}
