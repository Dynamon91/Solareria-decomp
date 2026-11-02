using Library;
using UnityEngine.Playables;

namespace SPR;

public class EnemyParameterActionBehaviour : PlayableBehaviour
{
	public Enemy Enemy;

	public string Method;

	public Parameter[] Parameters;

	private int duration;

	private int frameCount;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (!Method.IsNullOrEmpty() && Enemy.ROBeginParamActions.ContainsKey(Method))
		{
			Enemy.ROBeginParamActions[Method](Util.SecondsToFrame(playable.GetDuration()), Parameters);
		}
		duration = Util.SecondsToFrame(playable.GetDuration());
		frameCount = 0;
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (!Method.IsNullOrEmpty() && Enemy.ROParamActions.ContainsKey(Method))
		{
			Enemy.ROParamActions[Method]?.Invoke(Util.SecondsToFrame(playable.GetDuration()), Parameters);
		}
		if (frameCount == duration - 1 && !Method.IsNullOrEmpty() && Enemy.ROEndParamActions.ContainsKey(Method))
		{
			Enemy.ROEndParamActions[Method](Util.SecondsToFrame(playable.GetDuration()), Parameters);
		}
		frameCount++;
	}
}
