using System.Collections.Generic;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Achievement;
using SPR.Field;
using SPR.UI;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class MorphedImahen : StateSaveBreakableObject
{
	[Header("References")]
	[SerializeField]
	private GameObject figureObject;

	[SerializeField]
	private GameObject corpseObject;

	[SerializeField]
	private GameObject soundObject;

	[SerializeField]
	private List<CinemachineVirtualCamera> vcams;

	[SerializeField]
	private NarrationBoard narrationBoard;

	[SerializeField]
	private EventReference hitEvent;

	[Header("Values")]
	[SerializeField]
	private float duration;

	[SerializeField]
	private float interval;

	[SerializeField]
	private int maxTalk;

	private float timer;

	private int talkIndex;

	private PlayableDirector director;

	private MeshRenderer meshRenderer;

	private SkeletonAnimation spine;

	private Sequence visualKnockBackSequence;

	private static readonly int FillPhase = Shader.PropertyToID("_FillPhase");

	public override string ID => GetType().Name;

	private bool IsAlive => Count < BrokenState;

	private MeshRenderer MeshRenderer => meshRenderer ?? (meshRenderer = Spine.GetComponent<MeshRenderer>());

	public SkeletonAnimation Spine => spine ?? (spine = figureObject.GetComponentInChildren<SkeletonAnimation>(includeInactive: true));

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	protected override void Initialize()
	{
		base.Initialize();
		director = GetComponent<PlayableDirector>();
	}

	private void Update()
	{
		if (IsAlive)
		{
			if (timer >= duration + interval)
			{
				narrationBoard.SetScenario(GetNextTalk(out var actor, out var voiceKey));
				narrationBoard.SetVoice(actor, voiceKey);
				narrationBoard.SetActiveDuration(duration, duration / 4f);
				timer -= duration + interval;
			}
			timer += Time.deltaTime;
		}
	}

	protected override UniTask PlayBreakAnimationAsync()
	{
		Spine.SetAnimation("death");
		return default(UniTask);
	}

	protected override void OnLoadBrokenState()
	{
		SwitchToCorpse();
	}

	protected override void OnLoadNormalState()
	{
		figureObject.SetActive(value: true);
		corpseObject.SetActive(value: false);
		SetActiveIdleSfx(value: true);
		if (base.gameObject.activeInHierarchy)
		{
			Spine.SetAnimation("idle_infected", 0, loop: true, Spine.state.GetCurrent(0).TrackTime);
		}
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		PoolableTimeLimitObject poolableTimeLimitObject = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Enemy_Damaged.prefab", base.VisualCenter);
		poolableTimeLimitObject.Initialize("VFX/Enemy_Damaged.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		PoolableTimeLimitObject poolableTimeLimitObject2 = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Hit_Circle.prefab", base.VisualCenter);
		poolableTimeLimitObject2.Initialize("VFX/Hit_Circle.prefab", Util.GetCurrentAnimDuration(poolableTimeLimitObject2));
		Direction direction = ((!(base.VisualCenter.x - Game.Instance.Player.mover.VisualCenter.x < 0f)) ? Direction.Right : Direction.Left);
		VisualKnockBack(direction);
		DamageEffectAsync().Forget();
		return base.TryTakeDamage(hitInfo);
	}

	protected override void OnBreak()
	{
		vcams.ForEach(delegate(CinemachineVirtualCamera vcam)
		{
			vcam.transform.position = Camera.main.transform.position;
		});
		director.Play();
		SetActiveIdleSfx(value: false);
		narrationBoard.FadeOutNow(duration / 4f);
		CinemachineBasicMultiChannelPerlin noiseModule = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
		ShakeCamera(5f, 2f);
		UniTask.WaitForSeconds(7f / 60f).GetAwaiter().OnCompleted(delegate
		{
			ShakeCamera(0f, 0f);
		});
		Game.Instance.UIManager.MenuCanvas.MemoryPanel.UpdateMemory(BossName.Imahen, isClear: true);
		Game.Instance.SaveData.EnemyFightData.AddCreatureDeath("MorphedImahen");
		AchievementHelper.CheckTriggers();
		void ShakeCamera(float amplitude, float frequency)
		{
			noiseModule.m_AmplitudeGain = amplitude;
			noiseModule.m_FrequencyGain = frequency;
		}
	}

	protected override void PlaySfx(bool isBreak)
	{
		if (!isBreak)
		{
			BreakableObject.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		}
	}

	public void SwitchToCorpse()
	{
		figureObject.SetActive(value: false);
		corpseObject.SetActive(value: true);
	}

	private ScenarioData GetNextTalk(out string actor, out string voiceKey)
	{
		string id = $"Cutscene_ImahenLab_Loop_{1 + talkIndex++}";
		if (talkIndex >= maxTalk)
		{
			talkIndex -= maxTalk;
		}
		ScenarioData scenarioData = DataManager.Instance.GetScenarioData(id);
		actor = scenarioData.Actor;
		voiceKey = scenarioData.VoiceKey;
		return scenarioData;
	}

	private async UniTaskVoid DamageEffectAsync()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(FillPhase, 1f);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
		Sequence sequence = DOTween.Sequence();
		sequence.Append(DOFill(materialPropertyBlock, 1f, 5).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed)).Append(DOFill(materialPropertyBlock, 0f, 2).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed));
		await sequence.WaitAsync();
		Tween DOFill(MaterialPropertyBlock materialPropertyBlock2, float endValue, int frame)
		{
			return DOTween.To(() => materialPropertyBlock2.GetFloat(FillPhase), delegate(float x)
			{
				materialPropertyBlock2.SetFloat(FillPhase, x);
				MeshRenderer.SetPropertyBlock(materialPropertyBlock2);
			}, endValue, (float)frame / 60f);
		}
	}

	private void VisualKnockBack(Direction direction)
	{
		visualKnockBackSequence?.Kill();
		visualKnockBackSequence = DOTween.Sequence();
		Transform tr = MeshRenderer.transform;
		Vector3 initialPos = tr.localPosition;
		visualKnockBackSequence.Append(tr.DOLocalMoveX(0.3f * direction.ToVector2().x, 1f / 30f).SetEase(Ease.Linear)).Append(tr.DOLocalMoveX(0f, 1f / 30f).SetEase(Ease.Linear)).OnKill(delegate
		{
			tr.localPosition = initialPos;
		});
	}

	protected override void OnDrawGizmosSelected()
	{
	}

	public void SetActiveIdleSfx(bool value)
	{
		soundObject.SetActive(value);
	}
}
