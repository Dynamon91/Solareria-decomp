using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.L10N;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public class MapIconInfo : MonoBehaviour
{
	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private LocaleText flavorText;

	private bool hadIcon;

	private float duration;

	private Sequence fadeSeq;

	private CancellationTokenSource cancellationTokenSource;

	private CanvasGroup canvasGroup;

	private Animator animator;

	private static readonly int Open = Animator.StringToHash("Open");

	private static readonly int Close = Animator.StringToHash("Close");

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static WorldData WorldData => Game.Instance.SaveData.WorldData;

	public void Initialize(float fadeDuration)
	{
		base.gameObject.SetActive(value: false);
		duration = fadeDuration;
		hadIcon = false;
	}

	public void SetActive(MapIcon icon)
	{
		bool flag = (object)icon != null;
		if (flag)
		{
			Activate(icon);
		}
		else
		{
			DeactivateIfActivated();
		}
		hadIcon = flag;
	}

	private void Activate(MapIcon icon)
	{
		DeactivateIfActivated();
		ActivateAsync(icon, TaskUtil.RefreshToken(ref cancellationTokenSource)).Forget();
	}

	private async UniTask ActivateAsync(MapIcon icon, CancellationToken token)
	{
		await UniTask.WaitForSeconds(duration, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		if (hadIcon && SetData(icon))
		{
			base.gameObject.SetActive(value: true);
			Animator.ResetTrigger(Close);
			Animator.SetTrigger(Open);
		}
	}

	private void DeactivateIfActivated()
	{
		if (hadIcon)
		{
			cancellationTokenSource?.Cancel();
			Animator.ResetTrigger(Open);
			Animator.SetTrigger(Close);
		}
	}

	public void DeactivateNow()
	{
		cancellationTokenSource?.Cancel();
		Animator.Play("Focus icon info_wait");
		hadIcon = false;
	}

	private bool SetData(MapIcon icon)
	{
		if (!(icon is MapObjectIcon data))
		{
			if (icon is MapMarkerIcon data2)
			{
				return SetData(data2);
			}
			return false;
		}
		return SetData(data);
	}

	private bool SetData(MapObjectIcon icon)
	{
		string data = LocaleKey.MapLegendName(icon.Type);
		switch (icon.Type)
		{
		case MapObjectType.Altar:
			nameText.SetData(data);
			flavorText.SetRawText(Locale.GetAltarLocaleText(icon.FieldID));
			return true;
		case MapObjectType.CorestoneStatue:
			nameText.SetData(data);
			flavorText.ClearText();
			return true;
		case MapObjectType.FieldItem:
		case MapObjectType.ItemBox:
			nameText.SetData(data);
			flavorText.ClearText();
			return true;
		case MapObjectType.Information:
		{
			nameText.SetData(data);
			string text2 = icon.StringValue;
			string text3 = "Trigger_InformationMyoso".Replace("Trigger_", string.Empty);
			if (text2.StartsWith(text3))
			{
				text2 = text3;
			}
			flavorText.SetData(LocaleKey.ItemMapInfo(text2));
			return true;
		}
		case MapObjectType.Portal:
		{
			nameText.SetData(data);
			if (WorldData.TryGetObjectState(icon.FieldID.SectorName, icon.StringValue, out var state) && state == 1)
			{
				flavorText.SetData("UI_Map_Portal_" + icon.StringValue);
			}
			else
			{
				flavorText.ClearText();
			}
			return true;
		}
		case MapObjectType.Npc:
		{
			string text = icon.StringValue.Split("_")[2];
			nameText.SetData("Name_" + text);
			flavorText.SetData("Name_" + text + "_Job");
			return true;
		}
		case MapObjectType.SproutStone:
			nameText.SetData(data);
			flavorText.SetDataFormat("UI_Map_Icon_SproutStone_Format", Locale.GetLocaleString("Region_" + icon.StringValue.Replace("ShadowSproutStone_", string.Empty)));
			return true;
		case MapObjectType.Boss:
			nameText.SetData(data);
			flavorText.SetData($"Boss_{(BossName)icon.IntValue}");
			return true;
		case MapObjectType.FiveWarriors:
			nameText.SetData(data);
			flavorText.SetData($"Boss_{(BossName)icon.IntValue}");
			return true;
		case MapObjectType.Sprout:
		{
			nameText.SetData(data);
			string[] array = icon.StringValue.Split('_');
			FieldID fieldID = new FieldID(array[0].ToEnum<SectorName>(), array[1].ToInt());
			flavorText.SetDataFormat("UI_Map_Icon_Sprout_Format", Locale.GetLocaleString($"Region_{fieldID.GetRegionName()}"));
			return true;
		}
		case MapObjectType.Relics:
			nameText.SetData(data);
			flavorText.ClearText();
			return true;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private bool SetData(MapMarkerIcon icon)
	{
		int markerType = icon.MarkerType;
		if (markerType >= 0)
		{
			if (markerType > 0)
			{
				string markerName = MapMarkerIcon.GetMarkerName(icon.MarkerType);
				nameText.SetData(LocaleKey.ItemName(markerName));
				string rawText = Locale.GetLocaleString(LocaleKey.ItemDesc(markerName)).Split("\n")[0];
				flavorText.SetRawText(rawText);
				return true;
			}
			return false;
		}
		return false;
	}
}
