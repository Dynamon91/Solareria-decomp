using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using LapinLibrary;
using Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Field;

public class SolinLabRecordStone : InteractableObject
{
	[Header("Record Stone")]
	[SerializeField]
	private SpriteRenderer lightSpriteRenderer;

	[SerializeField]
	private EventReference onEvent;

	[SerializeField]
	private EventReference offEvent;

	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset activateTimeline;

	[SerializeField]
	private PlayableAsset deactivateTimeline;

	private PlayableDirector director;

	private CancellationTokenSource lightToken;

	private bool alwaysLightOn;

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		alwaysLightOn = InteractableObject.WorldData.HasInteractedFieldObject(base.ObjectID);
		if (alwaysLightOn)
		{
			Director.playableAsset = activateTimeline;
			Director.EvaluateAtLastFrame();
		}
	}

	protected override void OnInteract()
	{
		InteractableObject.InteractionPanel.OnActivate += delegate
		{
			PlayerTurningAnimationAsync(turnBack: true).Forget();
		};
		InteractableObject.InteractionPanel.OnDeactivate += delegate
		{
			PlayerTurningAnimationAsync(turnBack: false).Forget();
		};
		base.OnInteract();
		alwaysLightOn = true;
	}

	private void Update()
	{
		if (!alwaysLightOn && Director.state != PlayState.Playing)
		{
			Vector2 visualBottom = InteractableObject.Player.VisualBottom;
			Vector2 b = (Vector2)base.transform.position + base.BoxCollider.offset;
			bool value = Vector2.Distance(visualBottom, b) < base.BoxCollider.size.x / 2f + 0.5f;
			LightOnOffAsync(value).Forget();
		}
	}

	private async UniTask LightOnOffAsync(bool value)
	{
		PlayableAsset targetTimeline = (value ? activateTimeline : deactivateTimeline);
		EventReference sfxEvent = (value ? onEvent : offEvent);
		if (!(Director.playableAsset == targetTimeline))
		{
			await UniTask.WaitUntil(() => Director.state != PlayState.Playing, PlayerLoopTiming.Update, TaskUtil.RefreshToken(ref lightToken));
			if (!lightToken.IsCancellationRequested)
			{
				Director.playableAsset = targetTimeline;
				Director.Play();
				InteractableObject.SoundManager.PlayOneShotAttached(sfxEvent, base.transform);
			}
		}
	}

	private async UniTask PlayerTurningAnimationAsync(bool turnBack)
	{
		if (turnBack)
		{
			PlayerStateMachine psm = InteractableObject.Player.PSM;
			await UniTask.WaitUntil(() => psm.CurrentState == psm.IdleState);
			InteractableObject.Player.SetAnimation("turn_back", loop: false);
			InteractableObject.Player.AddAnimation("idle_back");
		}
		else
		{
			InteractableObject.Player.SetAnimation("turn_front", loop: false);
			InteractableObject.Player.AddAnimation("idle");
		}
	}
}
