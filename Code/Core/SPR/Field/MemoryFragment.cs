using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI.Map;
using UnityEngine;

namespace SPR.Field;

public class MemoryFragment : InteractableObject, IStateSavable, IMappable
{
	private Animator animator;

	private const int AcquiredState = 1;

	private static readonly int Acquire = Animator.StringToHash("Acquire");

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public string ID => base.ObjectID;

	protected override void OnInteract()
	{
		base.BoxCollider.enabled = false;
		base.OnInteract();
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem");
		ChangeState(1);
		InteractAsync().Forget();
	}

	private void Update()
	{
		Util.CheckShownInCamera(ID, base.transform.position);
	}

	private async UniTask InteractAsync()
	{
		Animator.SetTrigger(Acquire);
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
		base.gameObject.SetActive(value: false);
		MapNoticeData mapNoticeData = DataManager.Instance.GetMapNoticeData(ID);
		if (mapNoticeData != null)
		{
			MenuMapPanel.AddInfoStoneInformation(mapNoticeData.TargetID);
		}
	}

	public void LoadState(int state)
	{
		base.gameObject.SetActive(state == 0);
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		Vector3 position = base.transform.position;
		base.transform.position = position.RoundToInt();
		base.name = "MemoryFragment";
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 position = base.transform.position.ToVector2() + Vector2.down - center;
		return new MapObjectInfo(MapObjectType.FieldItem, position, ID);
	}
}
