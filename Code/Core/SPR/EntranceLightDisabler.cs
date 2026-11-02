using UnityEngine;

namespace SPR;

public class EntranceLightDisabler : MonoBehaviour
{
	[SerializeField]
	private CutsceneProducer targetCutscene;

	[SerializeField]
	private int targetState;

	private bool isChecked;

	public bool ShouldDisable()
	{
		isChecked |= targetCutscene.CurrentState < targetState && Game.Instance.InputManager.IsCurrentControlTarget(Game.Instance.Player);
		return isChecked;
	}
}
