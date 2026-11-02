using SPR.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Map;

public abstract class MapIcon : MonoBehaviour
{
	[SerializeField]
	protected Image iconImage;

	[SerializeField]
	protected Vector2 worldPositionConstant;

	protected const float GlobalScale = 5f;

	private bool isInitialized;

	private RectTransform rectTransform;

	public Vector2 WorldPositionConstant => worldPositionConstant;

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	protected static SaveData SaveData => Game.Instance.SaveData;

	protected abstract bool ShouldBeActive { get; }

	protected void ResetInitialize()
	{
		isInitialized = false;
	}

	protected void TryInitialize()
	{
		if (!isInitialized)
		{
			isInitialized = true;
		}
	}

	public bool CheckActive()
	{
		bool shouldBeActive = ShouldBeActive;
		TryInitialize();
		base.gameObject.SetActive(shouldBeActive);
		return shouldBeActive;
	}

	public void SetActive(bool value)
	{
		base.gameObject.SetActive(value);
		if (value)
		{
			OnSetActiveTrue();
		}
	}

	protected virtual void OnSetActiveTrue()
	{
	}
}
