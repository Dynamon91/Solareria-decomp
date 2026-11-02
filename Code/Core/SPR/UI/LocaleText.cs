using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Rewired;
using SPR.L10N;
using SPR.Library;
using TMPro;
using UnityEngine;

namespace SPR.UI;

public class LocaleText : MonoBehaviour, IControllerEventUpdate
{
	private enum TextComponentType
	{
		None,
		TextMeshPro,
		SuperTextMesh
	}

	[SerializeField]
	private TextType textType;

	[SerializeField]
	private string localeKey;

	[Header("Use Controller Change Event")]
	[SerializeField]
	private bool useEvent = true;

	private TextComponentType textComponentType;

	private TextMeshProUGUI tmp;

	private SuperTextMesh superTextMesh;

	private RectTransform rectTransform;

	public TextMeshProUGUI TMP => tmp ?? (tmp = GetComponent<TextMeshProUGUI>());

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private void Awake()
	{
		if (useEvent)
		{
			RewiredHelper.RegisterControllerChangeEvent(this);
		}
	}

	private void OnEnable()
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		SetFontSafeAsync().Forget();
		if (!localeKey.IsNullOrEmpty())
		{
			SetData();
		}
	}

	private void Initialize()
	{
		if (TryGetComponent<SuperTextMesh>(out superTextMesh))
		{
			textComponentType = TextComponentType.SuperTextMesh;
		}
		else if (TryGetComponent<TextMeshProUGUI>(out tmp))
		{
			textComponentType = TextComponentType.TextMeshPro;
		}
		else
		{
			DebugLog.GeneralLogError(base.gameObject.GetGameObjectPath() + "에는 STM도 Text 컴포넌트도 없습니다.");
		}
	}

	private async UniTask SetFontSafeAsync()
	{
		if (!Game.IsInitialized)
		{
			await UniTask.WaitUntil(() => Game.IsInitialized);
		}
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			tmp.font = Locale.GetLocaleTMPFont(textType);
		}
		else if (textComponentType == TextComponentType.SuperTextMesh)
		{
			superTextMesh.font = Locale.GetLocaleFont(textType);
		}
	}

	public void SetData(string key = null)
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		SetFontSafeAsync().Forget();
		if (key.IsNullOrEmpty())
		{
			key = localeKey;
		}
		string localeString = Locale.GetLocaleString(key);
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			tmp.text = localeString;
		}
		else if (textComponentType == TextComponentType.SuperTextMesh)
		{
			superTextMesh.text = localeString;
		}
	}

	public void ClearText()
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			tmp.text = string.Empty;
		}
		else if (textComponentType == TextComponentType.SuperTextMesh)
		{
			superTextMesh.text = string.Empty;
		}
	}

	[Obsolete("이거 쓰면 안될 것 같은데... (EQ1)")]
	public void SetRawText(string value)
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		SetFontSafeAsync().Forget();
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			tmp.text = value;
		}
		else if (textComponentType == TextComponentType.SuperTextMesh)
		{
			superTextMesh.text = value;
		}
	}

	public void SetScenario(string scenarioID)
	{
		ScenarioData scenarioData = DataManager.Instance.GetScenarioData(scenarioID);
		SetScenario(scenarioData);
	}

	public void SetScenario(ScenarioData data)
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		SetFontSafeAsync().Forget();
		string localedScenarioText = Locale.GetLocaledScenarioText(data, Game.CurrentLanguage);
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			tmp.text = localedScenarioText;
		}
		else if (textComponentType == TextComponentType.SuperTextMesh)
		{
			superTextMesh.text = localedScenarioText;
		}
	}

	public void SetDataFormat(string key, params string[] rawTexts)
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		SetFontSafeAsync().Forget();
		if (key.IsNullOrEmpty())
		{
			key = localeKey;
		}
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			tmp.text = Locale.GetLocaleFormat(key, rawTexts);
		}
		else if (textComponentType == TextComponentType.SuperTextMesh)
		{
			superTextMesh.text = Locale.GetLocaleFormat(key, rawTexts);
		}
	}

	public Color GetColor()
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			return tmp.color;
		}
		if (textComponentType == TextComponentType.SuperTextMesh)
		{
			return superTextMesh.color;
		}
		return default(Color);
	}

	public void SetColor(Color color)
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			tmp.color = color;
		}
		else if (textComponentType == TextComponentType.SuperTextMesh)
		{
			superTextMesh.color = color;
		}
	}

	public void SetAlpha(float a)
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			tmp.SetAlpha(a);
		}
		else if (textComponentType == TextComponentType.SuperTextMesh)
		{
			Color color = superTextMesh.color;
			color.a = Mathf.Clamp01(a);
			superTextMesh.color = color;
		}
	}

	public Tween DOAlpha(float endAlpha, float duration)
	{
		if (textComponentType == TextComponentType.None)
		{
			Initialize();
		}
		if (textComponentType == TextComponentType.TextMeshPro)
		{
			return tmp.DOAlpha(endAlpha, duration);
		}
		if (textComponentType == TextComponentType.SuperTextMesh)
		{
			return DOTween.ToAlpha(() => superTextMesh.color, delegate(Color color)
			{
				superTextMesh.color = color;
			}, endAlpha, duration);
		}
		return null;
	}

	public string GetCurrentText()
	{
		switch (textComponentType)
		{
		case TextComponentType.TextMeshPro:
			return tmp.text;
		case TextComponentType.SuperTextMesh:
			return superTextMesh.text;
		default:
			DebugLog.GeneralLogError("LocaleText(" + base.gameObject.name + ")의 타입이 정해지지 않았습니다!");
			return "";
		}
	}

	public void SetLocaleKey(string value)
	{
		localeKey = value;
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		if (useEvent && !string.IsNullOrEmpty(localeKey))
		{
			SetData();
		}
	}
}
