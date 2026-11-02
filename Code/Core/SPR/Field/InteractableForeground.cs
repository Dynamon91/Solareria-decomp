using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class InteractableForeground : Fog, IInteractable
{
	[SerializeField]
	private bool activeOnLeft = true;

	[SerializeField]
	private bool activeOnRight = true;

	public string Name => Util.GetID(Game.FieldID, "Interactable Foreground");

	public bool IsNull => base.gameObject.IsNull();

	public int InteractableOrder => 10;

	public void AwakeObject(SaveData saveData)
	{
		FadeOutAsync(instant: true).Forget();
	}

	public void Interact()
	{
	}

	public void InteractEnter()
	{
		FadeInAsync(instant: false).Forget();
	}

	public void InteractExit()
	{
		Vector3 position = Game.Instance.Player.transform.position;
		Vector2 vector = (Vector2)base.transform.position - coverAreaBox.Center;
		if ((activeOnLeft && position.x < vector.x + (float)coverAreaBox.LowerLeft.x) || (activeOnRight && vector.x + position.x > (float)coverAreaBox.UpperRight.x))
		{
			FadeOutAsync(instant: false).Forget();
		}
	}
}
