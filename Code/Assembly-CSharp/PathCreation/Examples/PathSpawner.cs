using UnityEngine;

namespace PathCreation.Examples;

public class PathSpawner : MonoBehaviour
{
	public PathCreator pathPrefab;

	public PathFollower followerPrefab;

	public Transform[] spawnPoints;

	private void Start()
	{
		Transform[] array = spawnPoints;
		foreach (Transform transform in array)
		{
			PathCreator pathCreator = Object.Instantiate(pathPrefab, transform.position, transform.rotation);
			Object.Instantiate(followerPrefab).pathCreator = pathCreator;
		}
	}
}
