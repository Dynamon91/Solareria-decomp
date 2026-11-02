using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SPR.Library;
using UnityEngine.SceneManagement;

namespace SPR;

public class VibrationManager : Manager
{
	public const float MaxVibration = 1f;

	private float currentLow = -1f;

	private float currentHigh = -1f;

	private List<VibrationRequest> activeRequests = new List<VibrationRequest>();

	private SettingData SettingData => Game.Instance.SettingData;

	protected override void OnInitialize()
	{
		StopAllVibration();
	}

	public override UniTask OnSceneLoaded(Scene loadedScene)
	{
		StopAllVibration();
		return UniTask.CompletedTask;
	}

	private void Update()
	{
	}

	private void SetMotorSpeeds(float low, float high)
	{
	}

	public Guid TriggerVibration(float low, float high, float duration)
	{
		VibrationRequest item = new VibrationRequest(low, high, duration);
		activeRequests.Add(item);
		return item.Id;
	}

	public void StopVibration(Guid id)
	{
		activeRequests.RemoveAll((VibrationRequest r) => r.Id == id);
	}

	public void StopAllVibration()
	{
		activeRequests.Clear();
		SetMotorSpeeds(0f, 0f);
	}

	private void OnApplicationQuit()
	{
		if (RewiredHelper.Player != null)
		{
			RewiredHelper.Player.StopVibration();
		}
	}
}
