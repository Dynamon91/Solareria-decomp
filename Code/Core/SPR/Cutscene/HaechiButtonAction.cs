using Cysharp.Threading.Tasks;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR.Cutscene;

public class HaechiButtonAction : ButtonActionBase
{
	public const string IdleAnimation = "scene/heachi_burst_charge_loop";

	private const string ChargingAnimation = "scene/heachi_burst_charging_loop";

	[Space]
	[Header("Haechi")]
	[SerializeField]
	private float mixDuration = 0.2f;

	private SkeletonAnimation PlayerSpine => Game.Instance.Player.mover.SkeletonAnimation;

	protected override void OnSuccess()
	{
		base.CutsceneProducer.PlayTimelineAsync(1, blockControl: true).Forget();
	}

	protected override void OnFail()
	{
		((HaechiRescueCutsceneProducer)base.CutsceneProducer).EscapeButtonAction();
	}

	protected override void OnControl()
	{
		if (Gauge <= 0f)
		{
			Gauge = 0f;
		}
		else
		{
			Timer = 0f;
		}
	}

	protected override void OnPull()
	{
		PlayerSpine.SetAnimation("scene/heachi_burst_charging_loop", 0, loop: true, 0f, 1f, isImmediate: false, mixDuration);
	}

	protected override void OnNotPull()
	{
		PlayerSpine.SetAnimation("scene/heachi_burst_charge_loop", 0, loop: true, 0f, 1f, isImmediate: false, mixDuration);
	}
}
