using System.Collections.Generic;
using StaticData;
using UnityEngine;

namespace SPR;

[CreateAssetMenu]
public class GameControlParameter : ScriptableObject
{
	private static GameControlParameter instance;

	[Header("Projectile")]
	[SerializeField]
	private float reflectedProjectileDistance;

	[SerializeField]
	private List<Difficulty> additionalPlatformDifficulties;

	public static GameControlParameter Instance
	{
		get
		{
			if ((object)instance == null)
			{
				instance = Resources.Load<GameControlParameter>("Game Control Parameter");
			}
			return instance;
		}
	}

	public float ReflectedProjectileDistance => reflectedProjectileDistance;

	public bool ShouldShowAdditionalPlatform(Difficulty difficulty)
	{
		return additionalPlatformDifficulties.Contains(difficulty);
	}
}
