using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR;

public abstract class Manager : MonoBehaviour
{
	private bool isInitialized;

	public void Initialize(bool force = false)
	{
		if (force)
		{
			isInitialized = false;
		}
		if (!isInitialized)
		{
			isInitialized = true;
			OnInitialize();
		}
	}

	protected abstract void OnInitialize();

	public abstract UniTask OnSceneLoaded(Scene loadedScene);
}
