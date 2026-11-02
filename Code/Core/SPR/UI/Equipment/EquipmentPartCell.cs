using Coffee.UIExtensions;
using DG.Tweening;
using Library;
using SPR.ObjectPool;
using SPR.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Equipment;

public class EquipmentPartCell : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private PartType partType;

	[SerializeField]
	private Image image;

	[SerializeField]
	private Image cursorImage;

	[SerializeField]
	private Image activeBackgroundImage;

	[SerializeField]
	private GameObject notiIcon;

	[SerializeField]
	private TextMeshProUGUI rankTextMeshPro;

	[SerializeField]
	private Image rankBackground;

	[SerializeField]
	private UIParticle equipParticle;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public PartType PartType => partType;

	public string PartID { get; private set; }

	public string PrefabKey => "Part Cell.prefab";

	public GameObject GameObject => base.gameObject;

	public void Initialize()
	{
		Focus(value: false);
		Clear();
	}

	public void SetData(string id, PartType partType, Sprite sprite)
	{
		PartID = id;
		this.partType = partType;
		UpdateLevel();
		image.sprite = sprite;
		image.gameObject.SetActive(value: true);
	}

	public void UpdateLevel()
	{
		if (!rankTextMeshPro.IsNull())
		{
			int partLevel = Game.Instance.SaveData.PlayerData.GetPartLevel(PartID);
			rankTextMeshPro.text = ((partLevel == 0) ? "" : $"+{partLevel}");
			rankTextMeshPro.color = Color.white;
			rankBackground.enabled = partLevel > 0;
		}
	}

	public void Clear()
	{
		PartID = null;
		image.sprite = null;
		image.gameObject.SetActive(value: false);
		SetBackgroundActive(value: false);
	}

	public void Focus(bool value)
	{
		if (value)
		{
			WanderingData.RemoveUnwatchedPart(partType, PartID);
			SetNotiIconActive(value: false);
		}
		cursorImage.gameObject.SetActive(value);
	}

	public void SetBackgroundActive(bool value)
	{
		activeBackgroundImage.gameObject.SetActive(value);
	}

	public void SetNotiIconActive(bool value)
	{
		if (!notiIcon.IsNull())
		{
			notiIcon.SetActive(value);
		}
	}

	public void PlayParticle()
	{
		equipParticle.Play();
	}

	public void ShakeImage()
	{
		DOTween.Kill(image.rectTransform, complete: true);
		image.rectTransform.DOShakePosition(1f, new Vector3(10f, 0f), 10, 90f, snapping: false, fadeOut: true, ShakeRandomnessMode.Harmonic);
	}

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
		Initialize();
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
