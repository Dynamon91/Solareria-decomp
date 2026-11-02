using UnityEngine;

namespace SPR;

public class EnvironmentTriggerActivator : MonoBehaviour
{
	[SerializeField]
	private bool isInverse;

	[SerializeField]
	private string triggerID;

	private void Start()
	{
		bool flag = Trigger.CheckTrigger(triggerID);
		base.gameObject.SetActive(isInverse ? (!flag) : flag);
	}
}
