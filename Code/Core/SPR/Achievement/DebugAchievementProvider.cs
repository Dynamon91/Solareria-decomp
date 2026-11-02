using Library;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR.Achievement;

public class DebugAchievementProvider : IAchievementProvider
{
	private AchievementToastUI toastInstance;

	private bool isInitialized;

	public void Initialize()
	{
		if (!isInitialized)
		{
			isInitialized = true;
			AsyncOperationHandle<GameObject> asyncOperationHandle = Addressables.InstantiateAsync("UI/AchievementToast");
			asyncOperationHandle.Completed += OnUILoaded;
		}
	}

	private void OnUILoaded(AsyncOperationHandle<GameObject> handle)
	{
		if (handle.Status == AsyncOperationStatus.Succeeded)
		{
			GameObject result = handle.Result;
			toastInstance = result.GetComponentInChildren<AchievementToastUI>();
			Object.DontDestroyOnLoad(result);
			DebugLog.AchievementLog("디버그용 업적 UI 로드 완료");
		}
		else
		{
			Debug.LogError("업적 UI 로드 실패: UI/AchievementToast");
		}
	}

	public void UnlockAchievement(string achievementId)
	{
		DebugLog.AchievementLog("[DEBUG] 업적 달성 성공! ID: " + achievementId);
		if (!toastInstance.IsNull())
		{
			toastInstance.Show(achievementId);
		}
	}
}
