using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class SpineCutscene : MonoBehaviour
{
	private PlayableDirector director;

	private SkeletonGraphic skeletonGraphic;

	private RectTransform rectTransform;

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	private SkeletonGraphic SkeletonGraphic => skeletonGraphic ?? GetComponent<SkeletonGraphic>();

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public bool FadeIn { get; set; }

	public bool IsEnd => Director.state != PlayState.Playing;

	public void AdjustScale()
	{
		float x = 1920f / SkeletonGraphic.SkeletonData.Width;
		float y = 1080f / SkeletonGraphic.SkeletonData.Height;
		RectTransform component = GetComponent<RectTransform>();
		float num = 1920f * component.pivot.x;
		float num2 = 1080f * component.pivot.y;
		component.localScale = new Vector3(x, y, 1f);
		component.anchoredPosition = new Vector3(num - 960f, num2 - 540f);
		Color color = SkeletonGraphic.color;
		SkeletonGraphic.color = new Color(color.r, color.g, color.b, 0f);
	}

	public void Play()
	{
		Director.Play();
	}
}
