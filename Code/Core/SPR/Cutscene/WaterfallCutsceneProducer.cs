namespace SPR.Cutscene;

public class WaterfallCutsceneProducer : SceneMontageCutsceneProducer
{
	protected override bool SkipCondition
	{
		get
		{
			if (base.SkipCondition)
			{
				return CutsceneProducer.FadeCanvas.IsWhite;
			}
			return false;
		}
	}
}
