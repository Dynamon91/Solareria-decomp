using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class VeritaArrowPortal : MonoBehaviour, IPoolableObject
{
	private int currentFrame;

	public string PrefabKey => "Verita Arrow Portal.prefab";

	public GameObject GameObject => base.gameObject;

	public void InitializeBeforeUse()
	{
		currentFrame = -1;
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void SetTimer(int frameCount)
	{
		currentFrame = frameCount;
	}

	private void FixedUpdate()
	{
		int num = currentFrame;
		if (num >= 0)
		{
			if (num == 0)
			{
				ReturnToPool();
			}
			else
			{
				currentFrame--;
			}
		}
	}
}
