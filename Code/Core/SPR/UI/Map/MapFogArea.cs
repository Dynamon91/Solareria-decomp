using Library;
using SPR.Library;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public class MapFogArea : MonoBehaviour
{
	[SerializeField]
	private bool autoIndex = true;

	[SerializeField]
	private int fogIndex;

	[SerializeField]
	private AreaBox areaBox;

	private static FieldID FieldID => Game.Instance.CurrentFieldID;

	private static SectorData SectorData
	{
		get
		{
			Game.Instance.SaveData.WorldData.TryGetSectorData(out var data);
			return data;
		}
	}

	private void Start()
	{
		if (SectorData.IsFogRevealed(FieldID.SceneIndex, fogIndex))
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void FixedUpdate()
	{
		if (!Game.Instance.Player.isDead && PhysicsUtility.IsPlayerStay(areaBox.Center, areaBox.Size))
		{
			SectorData.AddRevealedFog(FieldID.SceneIndex, fogIndex);
			Game.Instance.UIManager.MenuCanvas.MapPanel.CheckAchievementCleared();
			base.gameObject.SetActive(value: false);
		}
	}

	public MapFogInfo GetMapFogInfo()
	{
		return new MapFogInfo(fogIndex, areaBox.LowerLeft, areaBox.UpperRight);
	}

	private void OnDrawGizmos()
	{
		DrawGizmos(new Color(0.75f, 0.5f, 0.5f, 0.1f));
	}

	private void OnDrawGizmosSelected()
	{
		DrawGizmos(new Color(0.25f, 0.75f, 0.25f));
	}

	private void DrawGizmos(Color color)
	{
		if (autoIndex)
		{
			fogIndex = base.transform.GetSiblingIndex();
		}
		base.gameObject.name = $"Map Fog [{fogIndex}]";
		Gizmos.color = color;
		Gizmos.DrawWireCube(areaBox.Center, areaBox.Size.ToVector2());
	}
}
