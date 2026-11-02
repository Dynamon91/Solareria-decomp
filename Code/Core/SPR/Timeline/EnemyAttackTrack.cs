using System.Collections.Generic;
using Library;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(1f, 0f, 0.2509804f)]
[TrackClipType(typeof(EnemyAttackClip))]
[TrackBindingType(typeof(Enemy))]
public class EnemyAttackTrack : TrackAsset
{
	private readonly List<IDamageable> sharedDamageableList = new List<IDamageable>();

	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		EnemyAttackClipBase enemyAttackClipBase = clip.asset.CastObject<EnemyAttackClipBase>();
		sharedDamageableList.Clear();
		enemyAttackClipBase.SharedDamageableList = sharedDamageableList;
		return base.CreatePlayable(graph, gameObject, clip);
	}
}
