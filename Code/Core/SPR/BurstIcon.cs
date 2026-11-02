using Cysharp.Threading.Tasks;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class BurstIcon : MonoBehaviour, IPoolableObject
{
	private Animator animator;

	public string PrefabKey => "Burst Icon.prefab";

	public GameObject GameObject => base.gameObject;

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Highlight()
	{
		Animator.Play("highlight");
	}

	public async UniTaskVoid Disable()
	{
		Animator.Play("disable");
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).IsName("disable") && Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
		ReturnToPool();
	}
}
