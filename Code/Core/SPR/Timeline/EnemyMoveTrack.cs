using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(0f, 0.2509804f, 1f)]
[TrackClipType(typeof(EnemyMoveClip))]
[TrackBindingType(typeof(Enemy))]
public class EnemyMoveTrack : TrackAsset
{
}
