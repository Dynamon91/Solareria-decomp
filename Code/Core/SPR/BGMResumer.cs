using System.Linq;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class BGMResumer : MonoBehaviour, IStateSavable
{
	private const int ActivatedState = 1;

	private BoxCollider2D boxCollider;

	public string ID => $"BGMResumer_{base.transform.position.ToVector2Int()}";

	private BoxCollider2D BoxCollider => boxCollider ?? (boxCollider = GetComponent<BoxCollider2D>());

	private void FixedUpdate()
	{
		if (!Game.Instance.InputManager.IsCurrentControlTarget(Game.Instance.Player) || AttackUtility.OverlapBoxAll(base.transform.position, BoxCollider.size, LayerUtility.PlatformLayer).Any((Collider2D c) => c.TryGetComponent<BreakableWall>(out var _)))
		{
			return;
		}
		Game.Instance.RuntimeData.SetBGMLessEvent(value: false);
		Game.Instance.SoundManager.PlayBGM();
		if (Game.Instance.CurrentFieldID.Equals(SectorName.SilentRuins, 1))
		{
			Game.Instance.UIManager.FieldCanvas.SectorBoard.TryAppear(SectorName.SilentRuins, (Vector2 playerPos) => playerPos.x > 120f, isForced: true);
		}
		base.gameObject.SetActive(value: false);
		ChangeState(1);
	}

	private void OnDrawGizmos()
	{
		Vector3 position = base.transform.position;
		Vector3 size = new Vector3(1f, 5f, 0f);
		Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
		Gizmos.DrawCube(position, size);
	}

	public void LoadState(int state)
	{
		DebugLog.GeneralLog("LoadState 호출!");
		base.gameObject.SetActive(state == 0);
	}

	public void ChangeState(int state)
	{
		DebugLog.GeneralLog("ChangeState 호출!");
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			DebugLog.GeneralLog($"저장합니다! ({ID}, {state == 0})");
			data.SaveObjectState(ID, state);
		}
	}
}
