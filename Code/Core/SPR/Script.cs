using SPR.UI;
using SPR.UI.Field;
using SPR.UI.Talk;

namespace SPR;

public abstract class Script
{
	protected static FieldController FieldController => Game.Instance.FieldController;

	protected static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	protected static FadeCanvas FadeCanvas => Game.Instance.UIManager.FadeCanvas;

	protected static InputManager InputManager => Game.Instance.InputManager;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected static int SpeechIndex => (int)(Game.CurrentLanguage + 2);

	protected static int ScenarioENIndex => 4;

	protected static int ScenarioKOIndex => 3;

	public abstract bool IsInstant { get; }

	public abstract void Run(TalkCanvas talkCanvas);

	public abstract void Skip(TalkCanvas talkCanvas);
}
