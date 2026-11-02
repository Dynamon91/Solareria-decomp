using Cysharp.Threading.Tasks;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR.Cutscene;

public class ImahenButtonAction : ButtonActionBase
{
	[Space]
	[Header("Imahen")]
	[SerializeField]
	private SkeletonAnimation totSpine;

	[SerializeField]
	private SkeletonAnimation chasaSpine;

	[SerializeField]
	private float totMixDuration = 0.3f;

	[SerializeField]
	private float chasaMixDuration = 0.2f;

	private const string TotPullAnimation = "scene/imahen_lever_pull_loop";

	public const string TotNotPullAnimation = "scene/imahen_lever_pull_ready";

	private const string ChasaPushAnimation = "scene/imahen/lever_push";

	private const string ChasaNotPushAnimation = "scene/imahen/lever_idle";

	protected override void OnSuccess()
	{
		base.CutsceneProducer.PlayTimelineAsync(6, blockControl: true).Forget();
	}

	protected override void OnFail()
	{
		base.CutsceneProducer.PlayTimelineAsync(4, blockControl: true).Forget();
	}

	protected override void OnPull()
	{
		totSpine.SetAnimation("scene/imahen_lever_pull_loop", 0, loop: true, 0f, 1f, isImmediate: false, totMixDuration);
		chasaSpine.SetAnimation("scene/imahen/lever_push", 0, loop: true, 0f, 1f, isImmediate: false, chasaMixDuration);
	}

	protected override void OnNotPull()
	{
		totSpine.SetAnimation("scene/imahen_lever_pull_ready", 0, loop: true, 0f, 1f, isImmediate: false, totMixDuration);
		chasaSpine.SetAnimation("scene/imahen/lever_idle", 0, loop: true, 0f, 1f, isImmediate: false, chasaMixDuration);
	}
}
