using UnityEngine;

namespace SPR.Cutscene;

public class CharonCutsceneTrigger : MonoBehaviour
{
	private CharonCutsceneProducer producer;

	private CharonCutsceneProducer Producer => producer ?? (producer = GetComponentInParent<CharonCutsceneProducer>());

	public bool IsTriggered { get; private set; }

	public bool CheckTrigger()
	{
		return IsTriggered;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.TryGetComponent<PlayerMover>(out var _))
		{
			IsTriggered = true;
			base.gameObject.SetActive(value: false);
		}
	}
}
