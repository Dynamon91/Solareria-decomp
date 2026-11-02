using Library;
using UnityEngine;

namespace SPR.Field;

public class BreakableEnvironment : BreakableObject
{
	[SerializeField]
	private string breakAnimationName;

	public override HitType HitType => HitType.Object | HitType.Environment;

	private static RuntimeData RuntimeData => Game.Instance.RuntimeData;

	protected override string BreakAnimationName => breakAnimationName;

	private void Start()
	{
		if (RuntimeData.IsAlreadyBrokenEnvironment(Game.Instance.CurrentFieldID, base.VisualCenter) && !this.IsNull())
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void OnBreak()
	{
		RuntimeData.AddBrokenEnvironment(Game.Instance.CurrentFieldID, base.VisualCenter);
	}
}
