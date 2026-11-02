using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class NarrationClip : PlayableAsset
{
	public float fadeDuration = 0.6f;

	public string localeKey = string.Empty;

	public bool autoFade;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<NarrationBehaviour> scriptPlayable = ScriptPlayable<NarrationBehaviour>.Create(graph);
		NarrationBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.FadeDuration = fadeDuration;
		behaviour.LocaleKey = localeKey;
		behaviour.AutoFade = autoFade;
		return scriptPlayable;
	}
}
