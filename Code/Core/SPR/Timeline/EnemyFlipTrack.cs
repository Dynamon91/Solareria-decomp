using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(0f, 1f, 0.2509804f)]
[TrackClipType(typeof(EnemyFlipClip))]
[TrackBindingType(typeof(Enemy))]
public class EnemyFlipTrack : TrackAsset
{
}
