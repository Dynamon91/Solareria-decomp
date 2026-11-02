using System;
using SPR.Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

[Serializable]
public class HoldInput
{
	[SerializeField]
	private Image radialImage;

	private Timer holdTimer;

	public void Initialize(float duration)
	{
		holdTimer = new Timer(duration);
		radialImage.fillAmount = 0f;
	}

	public bool CheckInput(bool input, bool ignoreTimeScale = false)
	{
		if (holdTimer == null || holdTimer.Duration <= 0f)
		{
			return input;
		}
		if (input)
		{
			float deltaTime = (ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
			holdTimer.Tick(deltaTime);
			radialImage.fillAmount = holdTimer.TimePassed / holdTimer.Duration;
		}
		else
		{
			holdTimer.Reset();
			radialImage.fillAmount = 0f;
		}
		return holdTimer.IsDone;
	}
}
