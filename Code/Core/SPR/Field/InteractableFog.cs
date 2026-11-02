using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class InteractableFog : Fog, IInteractable, IStateSavable
{
	private BoxCollider2D coll;

	private const int DefaultState = 0;

	private const int DisappearState = 1;

	public string Name => "InteractableFog";

	public bool IsNull => base.gameObject.IsNull();

	public int InteractableOrder => 10;

	public string ID => Util.GetID(Game.FieldID, Name, base.transform.position.ToVector2Int());

	private BoxCollider2D Coll => coll ?? (coll = GetComponent<BoxCollider2D>());

	public void AwakeObject(SaveData saveData)
	{
	}

	public void Interact()
	{
	}

	public void InteractEnter()
	{
		Coll.enabled = false;
		FadeInAsync(instant: false).Forget();
		ChangeState(1);
	}

	public void InteractExit()
	{
	}

	public void LoadState(int state)
	{
		bool flag = state == 0;
		Coll.enabled = flag;
		if (flag)
		{
			FadeOutAsync(instant: true).Forget();
		}
		else
		{
			FadeInAsync(instant: true).Forget();
		}
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
		base.OnDrawGizmosSelected();
		Vector3 localScale = base.transform.localScale;
		Coll.size = coverAreaBox.Size.ToVector2() / localScale;
	}
}
