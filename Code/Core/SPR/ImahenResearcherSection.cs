using System.Collections.Generic;
using Library;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class ImahenResearcherSection : ImahenSection
{
	private const string ResearcherA1 = "Infected Researcher_A_1.prefab";

	private const string ResearcherA2 = "Infected Researcher_A_2.prefab";

	private const string ResearcherA3 = "Infected Researcher_A_3.prefab";

	private const string ResearcherB = "Infected Researcher_B_1.prefab";

	[SerializeField]
	private int initialCoolTimeFrame;

	[SerializeField]
	private int coolTimeFrame;

	[SerializeField]
	private int coolTimePenaltyCreatureCount;

	[SerializeField]
	private int coolTimePenalty;

	[SerializeField]
	private int spawnWaitCreatureCount;

	private readonly List<Vector2> spawnPoints = new List<Vector2>();

	private readonly List<string> spawnPool = new List<string>();

	private int frameCount;

	private int coolTime;

	private BoxCollider2D box2D;

	private readonly List<InfectedResearcher> researchers = new List<InfectedResearcher>();

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	private static int ActiveCount => ObjectPoolManager.GetActiveObjectCount("Infected Researcher_A_1.prefab") + ObjectPoolManager.GetActiveObjectCount("Infected Researcher_A_2.prefab") + ObjectPoolManager.GetActiveObjectCount("Infected Researcher_A_3.prefab") + ObjectPoolManager.GetActiveObjectCount("Infected Researcher_B_1.prefab");

	public override void Initialize()
	{
		foreach (PathFinder.Node node in PathFinder.NodeList)
		{
			if (node.Position.x > Box2D.bounds.min.x && node.Position.x < Box2D.bounds.max.x && node.Position.y > Box2D.bounds.min.y && node.Position.y < Box2D.bounds.max.y)
			{
				spawnPoints.Add(node.Position);
			}
		}
		spawnPool.Add("Infected Researcher_A_1.prefab");
		spawnPool.Add("Infected Researcher_A_2.prefab");
		spawnPool.Add("Infected Researcher_A_3.prefab");
		spawnPool.Add("Infected Researcher_B_1.prefab");
		coolTime = initialCoolTimeFrame;
		IsStopped = false;
	}

	public override void OnClear()
	{
		IsStopped = true;
		foreach (InfectedResearcher researcher in researchers)
		{
			researcher.TryTakeDamage(new HitInfo(Game.Instance.Player.mover, 999999f, 0f, 0f, AttackDirection.Facing, AttackAttribute.Default));
		}
	}

	private void FixedUpdate()
	{
		if (!IsStopped && ActiveCount < spawnWaitCreatureCount)
		{
			if (frameCount >= coolTime + ((ActiveCount >= coolTimePenaltyCreatureCount) ? coolTimePenalty : 0))
			{
				Spawn();
				coolTime = coolTimeFrame;
				frameCount = 0;
			}
			frameCount++;
		}
	}

	private void Spawn()
	{
		string key = spawnPool[CryptoRandom.Range(0, spawnPool.Count)];
		Vector2 groundPosition = spawnPoints[Random.Range(0, spawnPoints.Count)];
		ImahenArea componentInParent = GetComponentInParent<ImahenArea>();
		Vector3 vector = new Vector3(groundPosition.x, componentInParent.AreaBox.UpperRight.y);
		InfectedResearcher infectedResearcher = ObjectPoolManager.GetObject<InfectedResearcher>(key, vector, base.transform.parent);
		infectedResearcher.InitializeOnInstantiate(ActiveCount, vector);
		infectedResearcher.StateMachine.ChangeState(new CreatureFallingState(infectedResearcher, groundPosition));
		if (!researchers.Contains(infectedResearcher))
		{
			researchers.Add(infectedResearcher);
		}
	}
}
