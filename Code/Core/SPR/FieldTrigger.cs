using Library;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class FieldTrigger : MonoBehaviour, IInteractable
{
	private BoxCollider2D boxCollider;

	private BoxCollider2D BoxCollider2D => boxCollider ?? (boxCollider = GetComponent<BoxCollider2D>());

	protected virtual Vector3 TriggerSize => BoxCollider2D.bounds.size;

	protected virtual Vector3 TriggerCenter => base.transform.position;

	public string Name => base.gameObject.name;

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 0;

	public virtual void AwakeObject(SaveData saveData)
	{
	}

	public virtual void Interact()
	{
	}

	public virtual void InteractEnter()
	{
	}

	public virtual void InteractExit()
	{
	}

	protected virtual void OnDrawGizmos()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(TriggerCenter, TriggerSize);
	}
}
