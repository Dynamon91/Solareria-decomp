using UnityEngine;

namespace SPR.Library;

public static class LayerUtility
{
	public static readonly int DefaultLayerIndex = LayerMask.NameToLayer("Default");

	public static readonly int PlayerLayerIndex = LayerMask.NameToLayer("Player");

	public static readonly int MonsterLayerIndex = LayerMask.NameToLayer("Monster");

	public static readonly int PlatformLayerIndex = LayerMask.NameToLayer("Platform");

	public static readonly int OneWayPlatformLayerIndex = LayerMask.NameToLayer("OneWayPlatform");

	public static readonly int ObjectLayerIndex = LayerMask.NameToLayer("Object");

	public static readonly int CharacterIndex = LayerMask.NameToLayer("Character");

	public static readonly int TrapIndex = LayerMask.NameToLayer("Trap");

	public static readonly int ProjectileIndex = LayerMask.NameToLayer("Projectile");

	public static readonly int OhnLayerIndex = LayerMask.NameToLayer("Ohn");

	public static readonly LayerMask DefaultLayer = 1 << DefaultLayerIndex;

	public static readonly LayerMask PlayerLayer = 1 << PlayerLayerIndex;

	public static readonly LayerMask MonsterLayer = 1 << MonsterLayerIndex;

	public static readonly LayerMask PlatformLayer = 1 << PlatformLayerIndex;

	public static readonly LayerMask OneWayPlatformLayer = 1 << OneWayPlatformLayerIndex;

	public static readonly LayerMask ObjectLayer = 1 << ObjectLayerIndex;

	public static readonly LayerMask CharacterLayer = 1 << CharacterIndex;

	public static readonly LayerMask TrapLayer = 1 << TrapIndex;

	public static readonly LayerMask ProjectileLayer = 1 << ProjectileIndex;

	public static int EnemyDefaultAttackLayer => (int)PlayerLayer | (int)PlatformLayer | (int)TrapLayer;

	public static void SetLayer(this GameObject go, int layerIndex)
	{
		go.layer = layerIndex;
	}
}
