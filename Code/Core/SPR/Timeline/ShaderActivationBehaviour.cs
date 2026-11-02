using SPR.Graphic;
using SPR.Library;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class ShaderActivationBehaviour : PlayableBehaviour
{
	public GraphicManager.RenderFeatureName RenderFeatureName;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		GraphicHelper.SetRenderFeatureActive(RenderFeatureName, value: true);
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		GraphicHelper.SetRenderFeatureActive(RenderFeatureName, value: false);
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		GraphicHelper.SetRenderFeatureActive(RenderFeatureName, value: false);
	}
}
