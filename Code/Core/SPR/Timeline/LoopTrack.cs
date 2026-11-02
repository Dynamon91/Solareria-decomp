using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(1f, 0f, 1f)]
[TrackClipType(typeof(LoopClip))]
[TrackBindingType(typeof(CutsceneProducer))]
public class LoopTrack : TrackAsset
{
}
