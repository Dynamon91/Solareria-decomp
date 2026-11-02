using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LapinLibrary;
using Library;
using SPR.Save;
using SPR.UI.Tutorial;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class ParryActionDirector : MonoBehaviour
{
	[SerializeField]
	private Transform focusTarget;

	private PlayableDirector director;

	private ParryActionGuideCanvas guideCanvas;

	private CancellationTokenSource tokenSource;

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	private ParryActionGuideCanvas GuideCanvas => guideCanvas ?? (guideCanvas = GetComponentInChildren<ParryActionGuideCanvas>(includeInactive: true));

	private static InputManager InputManager => Game.Instance.InputManager;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private static TutorialCanvas TutorialCanvas => Game.Instance.UIManager.TutorialCanvas;

	private static TutorialMessageBoard TutorialMessageBoard => Game.Instance.UIManager.FieldCanvas.TutorialMessageBoard;

	public bool IsTutorialOn { get; private set; }

	public bool IsProducing => Director.state == PlayState.Playing;

	private static ActionName TutorialAction => ActionName.ParryAction;

	public async UniTask PlayTimelineAsync(Vector2 position, Action onEnd)
	{
		focusTarget.position = position;
		Director.Play();
		ShowTutorialMessage();
		CancellationToken token = TaskUtil.RefreshToken(ref tokenSource);
		await UniTask.WaitWhile(() => IsProducing && !token.IsCancellationRequested);
		Director.Stop();
		onEnd();
	}

	public void CancelPlayingTimeline()
	{
		tokenSource?.Cancel();
	}

	public void ShowTutorial()
	{
		if (WanderingData.ShouldShowTutorial(TutorialAction))
		{
			director.playableGraph.GetRootPlayable(0).SetSpeed(0.0);
			IsTutorialOn = true;
			SetCanvasPositionAsync().Forget();
			ShowTutorialAsync().Forget();
		}
	}

	private async UniTaskVoid ShowTutorialAsync()
	{
		TutorialCanvas.SetData(TutorialAction);
		await InputManager.StackControlTarget(TutorialCanvas);
		await UniTask.WaitWhile(() => InputManager.IsCurrentControlTarget(TutorialCanvas));
		GuideCanvas.Activate();
		await UniTask.WaitUntil(() => !InputManager.IsCurrentControlTarget(TutorialCanvas));
		TutorialMessageBoard.AddMessage(ActionName.ParryAction, ignoreTimeScale: true);
		PlayerStateMachine psm = Game.Instance.Player.mover.PSM;
		await UniTask.WaitUntil(() => psm.CurrentState == psm.PyronAttackState || psm.CurrentState == psm.HyperPyronAttackState);
		Game.SetTimeScale(1f);
		IsTutorialOn = false;
		GuideCanvas.Deactivate();
		WanderingData.SetTutorialDone(TutorialAction);
		if (director.playableGraph.IsValid())
		{
			director.playableGraph.GetRootPlayable(0).SetSpeed(1.0);
		}
	}

	private async UniTask SetCanvasPositionAsync()
	{
		Enemy enemy = UnityEngine.Object.FindObjectsOfType<Enemy>().FirstOrDefault((Enemy enemy2) => enemy2.StateMachine.CurrentState is EnemyBounceState);
		if (BasicExtension.IsNull(enemy))
		{
			DebugLog.GeneralLogWarning("패링 액션에 진입했지만 튕겨남 상태의 적이 없습니다!");
			return;
		}
		GuideCanvas.SetPosition(enemy);
		await UniTask.Yield();
	}

	private static void ShowTutorialMessage()
	{
		if (!WanderingData.ShouldShowTutorial(TutorialAction) && WanderingData.ShouldShowTutorialMessage(TutorialAction))
		{
			TutorialMessageBoard.AddMessage(TutorialAction);
		}
	}
}
