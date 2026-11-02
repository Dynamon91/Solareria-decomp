using Cysharp.Threading.Tasks;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class TycheBreathFog : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private string address;

	[SerializeField]
	private ParticleSystem particle;

	private FrameTimer cycleTimer;

	private FrameTimer durationTimer;

	private BoxCollider2D box2D;

	public string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public string Name { get; set; } = "TycheDarkBreathFog";

	public Direction FacingDirection => Direction.None;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
		cycleTimer = null;
		durationTimer = null;
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public async UniTaskVoid Activate(int cycle, int duration)
	{
		base.transform.position += new Vector3(0f, Box2D.size.y / 2f);
		cycleTimer = new FrameTimer(cycle);
		durationTimer = new FrameTimer(duration);
		while (!durationTimer.IsDone)
		{
			if (cycleTimer.IsDone)
			{
				HandleOverlap();
				cycleTimer.Reset();
			}
			cycleTimer.Tick();
			durationTimer.Tick();
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		await UniTask.WaitWhile(() => particle.isPlaying);
		ReturnToPool();
	}

	private void HandleOverlap()
	{
		AttackUtility.AttackBox(base.transform.position, Box2D.size * base.transform.localScale, new HitInfo(this, string.Empty), LayerUtility.PlayerLayer);
	}
}
