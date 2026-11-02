using Cysharp.Threading.Tasks;
using SPR.UI.Field;
using SPR.UI.Menu;
using SPR.UI.Option;
using SPR.UI.Rest;
using SPR.UI.Talk;
using SPR.UI.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR.UI;

public class UIManager : Manager
{
	[SerializeField]
	private FieldCanvas fieldCanvas;

	[SerializeField]
	private TalkCanvas talkCanvas;

	[SerializeField]
	private FadeCanvas fadeCanvas;

	[SerializeField]
	private MenuCanvas menuCanvas;

	[SerializeField]
	private RestCanvas restCanvas;

	[SerializeField]
	private TutorialCanvas tutorialCanvas;

	[SerializeField]
	private GameObject totFigureForUI;

	[SerializeField]
	private VideoCanvas videoCanvas;

	[SerializeField]
	private OptionCanvas optionCanvas;

	public FieldCanvas FieldCanvas => fieldCanvas;

	public TalkCanvas TalkCanvas => talkCanvas;

	public FadeCanvas FadeCanvas => fadeCanvas;

	public MenuCanvas MenuCanvas => menuCanvas;

	public RestCanvas RestCanvas => restCanvas;

	public TutorialCanvas TutorialCanvas => tutorialCanvas;

	public GameObject TotFigure => totFigureForUI;

	public VideoCanvas VideoCanvas => videoCanvas;

	public OptionCanvas OptionCanvas => optionCanvas;

	protected override void OnInitialize()
	{
		fieldCanvas.Initialize();
		talkCanvas.Initialize();
		fadeCanvas.Initialize();
		restCanvas.Initialize();
		menuCanvas.Initialize();
		tutorialCanvas.Initialize();
		optionCanvas.Initialize();
	}

	public override UniTask OnSceneLoaded(Scene loadedScene)
	{
		fieldCanvas.OnSceneLoaded();
		talkCanvas.OnSceneLoaded();
		return default(UniTask);
	}
}
