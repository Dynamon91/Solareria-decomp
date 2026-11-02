using System.Collections;
using UnityEngine;

namespace SPR.Field;

public class TerradinBigEntranceElevator : EntranceElevator
{
	protected override IEnumerator BeforeMoveRoutine()
	{
		yield return new WaitForSeconds(0.25f);
		PlayActivateAnimation();
		yield return new WaitUntil(() => elevatorAnimator.GetCurrentAnimatorStateInfo(0).IsName("Activated"));
		yield return new WaitUntil(() => elevatorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.99f);
	}
}
