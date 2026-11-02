using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public class MapFogPart : MonoBehaviour
{
	private const float GlobalScale = 5f;

	private const float Epsilon = 0.01f;

	[SerializeField]
	[ReadOnlyInspector]
	private FieldID fieldID;

	[SerializeField]
	[ReadOnlyInspector]
	private int index;

	private RectTransform rectTransform;

	private bool wasRevealed;

	private RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private SectorData SectorData
	{
		get
		{
			Game.Instance.SaveData.WorldData.TryGetSectorData(fieldID.SectorName, out var data);
			return data;
		}
	}

	public void Initialize()
	{
		wasRevealed = false;
	}

	public void SetActive()
	{
		bool flag = SectorData.IsFogRevealed(fieldID.SceneIndex, index);
		base.gameObject.SetActive(!flag);
		if (flag)
		{
			wasRevealed = true;
		}
	}

	public bool CheckRaycast(Vector2 worldPos)
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return false;
		}
		Vector2 vector = RectTransform.sizeDelta + new Vector2(0.01f, 0.01f);
		Vector3 lossyScale = RectTransform.lossyScale;
		Vector3 position = RectTransform.position;
		Vector2 vector2 = new Vector2(position.x - worldPos.x, position.y - worldPos.y);
		Vector2 vector3 = new Vector2(vector.x * lossyScale.x / 2f, vector.y * lossyScale.y / 2f);
		Vector2 vector4 = vector2 - vector3;
		Vector2 vector5 = vector2 + vector3;
		float num = vector4.x * vector5.x;
		float num2 = vector4.y * vector5.y;
		if (num < 0f || Mathf.Approximately(num, 0f))
		{
			if (!(num2 < 0f))
			{
				return Mathf.Approximately(num2, 0f);
			}
			return true;
		}
		return false;
	}

	public bool IsRevealed()
	{
		if (wasRevealed)
		{
			return true;
		}
		wasRevealed = SectorData.IsFogRevealed(fieldID.SceneIndex, index);
		return wasRevealed;
	}
}
