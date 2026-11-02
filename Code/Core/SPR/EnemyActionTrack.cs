using UnityEngine.Timeline;

namespace SPR;

[TrackColor(0.5019608f, 0.5019608f, 0.2509804f)]
[TrackClipType(typeof(EnemyActionClipBase))]
[TrackBindingType(typeof(Enemy))]
public class EnemyActionTrack : PlayableTrack
{
}
