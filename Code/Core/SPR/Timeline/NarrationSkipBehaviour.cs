using SPR.Library;
using SPR.UI;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

public class NarrationSkipBehaviour : PlayableBehaviour
{
	public PlayableDirector Director;

	public float SkipCheckTime;

	public TimelineClip Clip;

	private float elapsedTime;

	private bool skipped;

	private static NarrationBoard NarrationBoard => Game.Instance.UIManager.FieldCanvas.NarrationBoard;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		base.OnBehaviourPlay(playable, info);
		elapsedTime = 0f;
		skipped = false;
		NarrationBoard.EnableControlGuide(enable: true);
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (skipped && Director.time >= Clip.start + (double)SkipCheckTime - (double)(info.deltaTime * 2f) && Director.time < Clip.end)
		{
			Director.time = Clip.end;
		}
		else if (!skipped && !(elapsedTime >= SkipCheckTime))
		{
			if (RewiredHelper.Player.GetButtonDown("ConfirmUI"))
			{
				NarrationBoard.EnableControlGuide(enable: false);
				skipped = true;
			}
			elapsedTime += info.deltaTime;
		}
	}
}
