using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public abstract class EnemyAttackClipBase : PlayableAsset
{
	public bool showInGizmo;

	public string key;

	public int interval = 1;

	public bool isShared;

	public bool ignoreStun;

	public bool splitDirection;

	[HideInInspector]
	public List<IDamageable> SharedDamageableList;
}
