using Cysharp.Threading.Tasks;
using SPR.Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class FallenTiedPlatform : MonoBehaviour, IGround
{
	[SerializeField]
	private FieldID targetField;

	[SerializeField]
	private Vector2Int targetPosition;

	[SerializeField]
	private SoundManager.GroundMaterialType groundMaterialType;

	public SoundManager.GroundMaterialType GroundMaterial => groundMaterialType;

	private void Awake()
	{
		base.gameObject.SetActive(value: false);
		UniTask.WaitUntil(() => Game.Instance.FieldController.IsLoaded).GetAwaiter().OnCompleted(ChangeState);
	}

	private void ChangeState()
	{
		SectorData data;
		int state;
		bool flag = Game.Instance.SaveData.WorldData.TryGetSectorData(targetField.SectorName, out data) && data.TryGetObjectStateNum(string.Format("{0}_{1}_{2}", "TiedPlatform", targetField, targetPosition), out state) && state == 1;
		base.gameObject.SetActive(flag);
		if (flag)
		{
			TiedPlatform.DisableDamageables(AttackUtility.OverlapBoxAll(base.transform.position, base.transform.localScale, ~(int)LayerUtility.PlayerLayer));
			TiedPlatform.AddFallenPlatformToTilemap(base.transform.position, base.transform.localScale);
		}
	}

	private void OnDrawGizmos()
	{
		Vector3 position = base.transform.position;
		base.transform.position = new Vector3(Mathf.RoundToInt(position.x), (float)Mathf.RoundToInt(position.y * 2f) / 2f, position.z);
	}
}
