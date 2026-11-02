using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public class WipWall : MonoBehaviour
{
	private static UIManager UIManager => Game.Instance.UIManager;

	private static InputManager InputManager => Game.Instance.InputManager;

	private void OnCollisionEnter2D(Collision2D coll)
	{
		if (coll.gameObject.TryGetComponent<PlayerMover>(out var _))
		{
			UIManager.FieldCanvas.InteractionPanel.ShowDataImmediately(ItemType.None, "WIP");
		}
	}
}
