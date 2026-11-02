using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class TriggerDoor : MonoBehaviour
{
	[SerializeField]
	private TriggerData trigger;

	private void Awake()
	{
		BoxCollider2D component = GetComponent<BoxCollider2D>();
		SkeletonAnimation componentInChildren = GetComponentInChildren<SkeletonAnimation>(includeInactive: true);
		bool flag = Trigger.CheckTrigger(trigger);
		component.enabled = !flag;
		componentInChildren.SetAnimation(flag ? "open_loop" : "close_loop");
	}

	private void OnDrawGizmosSelected()
	{
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
	}
}
