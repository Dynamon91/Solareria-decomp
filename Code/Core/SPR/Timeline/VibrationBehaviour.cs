using System;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class VibrationBehaviour : PlayableBehaviour
{
	public float LowMotor;

	public float HighMotor;

	private Guid vibrationId;

	private bool isPlaying;

	private static VibrationManager VibrationManager => Game.Instance.VibrationManager;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (Application.isPlaying)
		{
			float duration = (float)playable.GetDuration();
			vibrationId = VibrationManager.TriggerVibration(LowMotor, HighMotor, duration);
			isPlaying = true;
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (isPlaying)
		{
			VibrationManager.StopVibration(vibrationId);
			isPlaying = false;
		}
	}
}
