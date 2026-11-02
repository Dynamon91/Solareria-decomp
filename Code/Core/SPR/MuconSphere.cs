using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using PathCreation;
using SPR.ObjectPool;
using SPR.UI;
using UnityEngine;

namespace SPR;

public class MuconSphere : MonoBehaviour, IPoolableObject, IDamageable
{
	[SerializeField]
	private string address;

	[Header("Stat")]
	[SerializeField]
	private int health;

	[Header("References")]
	[SerializeField]
	private ParticleSystem loop;

	[Header("FMOD")]
	[SerializeField]
	private EventReference finishEvent;

	private BoxCollider2D box2D;

	private CreatureCanvas creatureCanvas;

	private int currentHealth;

	private CancellationTokenSource cts;

	public string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter => base.transform.position;

	public bool IsDestroyed => (float)currentHealth <= 0f;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public void InitializeBeforeUse()
	{
		CreateCreatureCanvas().Forget();
		creatureCanvas.Initialize(Box2D.size.y, 0);
		base.gameObject.SetActive(value: true);
		currentHealth = health;
		cts = new CancellationTokenSource();
	}

	public void ReturnToPool()
	{
		cts.Cancel();
		cts = null;
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (IsDestroyed)
		{
			return false;
		}
		int num = currentHealth;
		currentHealth -= hitInfo.Damage;
		int delta = currentHealth - num;
		creatureCanvas.UpdateHealthBar(currentHealth, health, delta);
		if (IsDestroyed)
		{
			ReturnToPool();
		}
		return true;
	}

	public async UniTask Move(PathCreator pathCreator, float speed)
	{
		loop.Play(withChildren: true);
		float velocity = speed * Time.deltaTime;
		float travelled = 0f;
		while (base.transform.position != pathCreator.bezierPath[pathCreator.bezierPath.NumPoints - 1])
		{
			base.transform.position = pathCreator.path.GetPointAtDistance(travelled, EndOfPathInstruction.Stop);
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cts.Token);
			travelled += velocity;
		}
		loop.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		Game.Instance.ObjectPoolManager.GetObject<PoolingParticle>("Mucon_Sphere_Hit.prefab", base.transform.position);
		Game.Instance.SoundManager.PlayOneShotAttached(finishEvent, base.transform);
		ReturnToPool();
	}

	private async UniTaskVoid CreateCreatureCanvas()
	{
		if ((object)creatureCanvas == null)
		{
			creatureCanvas = await AddressableHelper.InstantiateAsync<CreatureCanvas>("Creature Canvas.prefab", base.transform);
		}
	}
}
