using Cinemachine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class CinemachineImpulseBehaviour : PlayableBehaviour
{
	public CinemachineImpulseSource ImpulseSource;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		ImpulseSource.m_ImpulseDefinition.m_ImpulseDuration = (float)playable.GetDuration();
		ImpulseSource.GenerateImpulse();
	}
}
