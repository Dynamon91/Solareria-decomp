using Library;
using UnityEngine.Playables;

namespace SPR;

public class EnemyActionBehaviour : PlayableBehaviour
{
	public string Method;

	public Enemy Enemy;

	private int frameCount;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (!Method.IsNullOrEmpty() && Enemy.ReadOnlyTimelineBeginActions.ContainsKey(Method))
		{
			Enemy.ReadOnlyTimelineBeginActions[Method](Util.SecondsToFrame(playable.GetDuration()));
		}
		frameCount = 0;
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (frameCount == Util.SecondsToFrame(playable.GetDuration()) && !Method.IsNullOrEmpty() && Enemy.ReadOnlyTimelineEndActions.ContainsKey(Method))
		{
			Enemy.ReadOnlyTimelineEndActions[Method](Util.SecondsToFrame(playable.GetDuration()));
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (!Method.IsNullOrEmpty() && Enemy.ReadOnlyTimelineActions.ContainsKey(Method))
		{
			Enemy.ReadOnlyTimelineActions[Method]?.Invoke(Util.SecondsToFrame(playable.GetDuration()));
		}
		frameCount++;
	}
}
