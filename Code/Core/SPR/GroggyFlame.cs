using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR;

public class GroggyFlame : IAttackSource
{
	private ParticleSystem particle;

	private Material outlineMaterial;

	private PoolableTimeLimitObject poolableObject;

	private const int DamageInterval = 15;

	private readonly float damageAmount = 20f * Util.FrameToSeconds(15f);

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	private static FieldController FieldController => Game.Instance.FieldController;

	public string Name => "GroggyFlame";

	public Direction FacingDirection => Direction.None;

	public GroggyFlame()
	{
		LoadAsync().Forget();
	}

	private async UniTask LoadAsync()
	{
		ObjectPoolManager.CreatePoolAsync("Temp_Heavy Blitz Flame.prefab", 1).Forget();
		AsyncOperationHandle<Material> matOp = Addressables.LoadAssetAsync<Material>("Temp_Blitz Flame Boss.mat");
		await matOp;
		outlineMaterial = matOp.Result;
	}

	private void Initialize(Vector2 pos, Vector2 size, float duration)
	{
		poolableObject = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Blitz_FireFloor.prefab", pos);
		poolableObject.Initialize("VFX/Blitz_FireFloor.prefab", duration);
		particle = poolableObject.GetComponent<ParticleSystem>();
		particle.Stop();
		poolableObject.gameObject.SetActive(value: true);
	}

	public async UniTaskVoid FlameAttackAsync(Enemy enemy, Vector2 pos, Vector2 size)
	{
		Material originMat = enemy.GetMaterial();
		int duration = enemy.Groggy.Duration;
		float frameCount = 0f;
		HitInfo hitInfo = new HitInfo(this, 300f, 0f, 0f, AttackDirection.Both, AttackAttribute.Default);
		Initialize(pos, size, duration);
		particle.Play();
		while (enemy.IsInGroggyState && !(frameCount > (float)duration))
		{
			if (frameCount % 15f == 0f)
			{
				enemy.TryTakeDamage(hitInfo);
				ChangeMaterialAsync(enemy, originMat).Forget();
			}
			await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
			frameCount += 1f;
		}
		if (poolableObject.gameObject.activeInHierarchy)
		{
			poolableObject.ReturnToPool();
		}
	}

	private async UniTask ChangeMaterialAsync(Enemy enemy, Material originMat)
	{
		enemy.SetOverrideMaterial(outlineMaterial);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetTexture("_MainTex", originMat.mainTexture);
		enemy.SetPropertyBlock(materialPropertyBlock);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(5);
		enemy.ResetMaterial();
	}
}
