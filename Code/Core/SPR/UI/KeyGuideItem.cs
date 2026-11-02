using Cysharp.Threading.Tasks;
using Library;
using Rewired;
using SPR.L10N;
using SPR.Library;
using TMPro;
using UnityEngine;

namespace SPR.UI;

public class KeyGuideItem : MonoBehaviour, IControllerEventUpdate
{
	[SerializeField]
	private string localeKey;

	private CanvasGroup canvasGroup;

	private TextMeshProUGUI tmp;

	private ILayoutRebuildParent layoutRebuildParent;

	private bool rebuildOnEnable;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private TextMeshProUGUI TMP => tmp ?? (tmp = GetComponent<TextMeshProUGUI>());

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	private void OnDestroy()
	{
		RewiredHelper.RemoveControllerChangeEvent(this);
	}

	private void OnEnable()
	{
		if (!Game.IsInitialized)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		SetFontSafeAsync().Forget();
		SetData_Internal(rebuildOnEnable);
	}

	private void SetData_Internal(bool rebuild)
	{
		if (!string.IsNullOrEmpty(localeKey))
		{
			SetData();
			if (rebuild)
			{
				layoutRebuildParent?.TryRebuild();
			}
		}
	}

	private async UniTask SetFontSafeAsync()
	{
		await UniTask.WaitUntil(() => Game.IsInitialized);
		TMP.font = Locale.GetLocaleTMPFont(TextType.Regular);
	}

	public void SetData(string key = null)
	{
		SetFontSafeAsync().Forget();
		if (key.IsNullOrEmpty())
		{
			key = localeKey;
		}
		else
		{
			localeKey = key;
		}
		TMP.text = Locale.GetLocaleString(key);
	}

	public void Activate()
	{
		base.gameObject.SetActive(value: true);
		CanvasGroup.alpha = 1f;
	}

	public void Deactivate()
	{
		CanvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: false);
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		SetData_Internal(rebuild: true);
	}

	public void RegisterLayoutRebuildParent(ILayoutRebuildParent parent, bool setRebuildOnEnable = false)
	{
		layoutRebuildParent = parent;
		rebuildOnEnable = setRebuildOnEnable;
	}
}
