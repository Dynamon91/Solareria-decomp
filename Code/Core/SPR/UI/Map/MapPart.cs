using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Map;

public class MapPart : MonoBehaviour
{
	[Serializable]
	private struct MapVariantInfo
	{
		[SerializeField]
		public int state;

		[SerializeField]
		public Vector2 offset;

		[SerializeField]
		public Sprite mask;

		[SerializeField]
		public Sprite outline;

		public MapVariantInfo(int state, Vector2 offset, Sprite mask, Sprite outline)
		{
			this.state = state;
			this.offset = offset;
			this.mask = mask;
			this.outline = outline;
		}

		public bool Equals(MapVariantInfo other)
		{
			if (state == other.state && offset.Equals(other.offset) && (object)mask == other.mask)
			{
				return (object)outline == other.outline;
			}
			return false;
		}
	}

	public const float MapGlobalScale = 5f;

	[SerializeField]
	private GameObject mapObjectIconPrefab;

	[SerializeField]
	private GameObject mapSpecialMarkerIconPrefab;

	[SerializeField]
	private GameObject mapFogPartPrefab;

	[SerializeField]
	private Image maskImage;

	[SerializeField]
	private Image outlineImage;

	[Header("Serialized Data")]
	[SerializeField]
	[ReadOnlyInspector]
	private Vector2 originPosition;

	[SerializeField]
	[ReadOnlyInspector]
	private string variantObjectId;

	[SerializeField]
	[ReadOnlyInspector]
	private List<MapVariantInfo> mapVariants;

	private FieldID fieldID;

	private RectTransform rectTransform;

	private const int InitialChildCount = 3;

	private bool wasVisited;

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public FieldID FieldID
	{
		get
		{
			if (fieldID.SectorName == SectorName.None)
			{
				fieldID = FieldID.ConvertToFieldID(base.name);
			}
			return fieldID;
		}
	}

	public void Initialize()
	{
		wasVisited = false;
	}

	public void Activate()
	{
		base.gameObject.SetActive(value: true);
		maskImage.alphaHitTestMinimumThreshold = 0.1f;
		outlineImage.alphaHitTestMinimumThreshold = 0.1f;
		wasVisited = true;
		if (!mapVariants.IsNullOrEmpty())
		{
			int state = 0;
			if (Game.Instance.SaveData.WorldData.TryGetSectorData(fieldID.SectorName, out var data) && data.TryGetObjectStateNum(variantObjectId, out var state2))
			{
				state = state2;
			}
			if (fieldID.Equals(SectorName.WatermirrorBasin, 25) && state < 4 && Game.Instance.SaveData.EnemyFightData.GetCreatureDeathCount("RoyalGuard") > 0)
			{
				state = 32;
			}
			MapVariantInfo mapVariantInfo = mapVariants.FirstOrDefault((MapVariantInfo v) => v.state == state);
			if (mapVariantInfo.Equals(default(MapVariantInfo)))
			{
				mapVariantInfo = mapVariants.First();
			}
			RectTransform.anchoredPosition = originPosition + mapVariantInfo.offset * 5f;
			maskImage.sprite = mapVariantInfo.mask;
			outlineImage.sprite = mapVariantInfo.outline;
		}
	}

	public void Deactivate()
	{
		base.gameObject.SetActive(value: false);
	}

	public Vector2 CalculateIconPosition(Vector2 normalizedPosition)
	{
		Vector2 sizeDelta = RectTransform.sizeDelta;
		float x = sizeDelta.x;
		float y = sizeDelta.y;
		Vector2 vector = RectTransform.anchoredPosition - new Vector2(x / 2f, y / 2f);
		Vector2 vector2 = new Vector2(normalizedPosition.x * x, normalizedPosition.y * y);
		return vector + vector2;
	}

	public bool IsVisited()
	{
		if (wasVisited)
		{
			return true;
		}
		wasVisited = Game.Instance.SaveData.WorldData.TryGetSectorData(fieldID.SectorName, out var data) && data.VisitedFieldIDList.Contains(fieldID.SceneIndex);
		return wasVisited;
	}
}
