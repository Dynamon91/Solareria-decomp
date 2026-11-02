using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SPR.UI;

public class VideoCanvas : AutoScaledCanvas
{
	[SerializeField]
	private RawImage videoRenderImage;

	[SerializeField]
	private VideoPlayer videoPlayer;

	public bool IsVideoPlaying => videoPlayer.isPlaying;

	public void SetVideo(VideoClip videoClip)
	{
		videoPlayer.clip = videoClip;
	}

	public async UniTask PrepareVideo(CancellationToken token)
	{
		videoPlayer.Prepare();
		await UniTask.WaitUntil(() => videoPlayer.isPrepared, PlayerLoopTiming.Update, token);
	}

	public void PlayVideo()
	{
		videoRenderImage.color = Color.white;
		videoPlayer.Play();
	}

	public async UniTask PlayVideoAsync(CancellationToken token)
	{
		await PrepareVideo(token);
		videoRenderImage.color = Color.white;
		videoPlayer.Play();
	}

	public void PauseVideo()
	{
		videoPlayer.Pause();
	}

	public void StopVideo()
	{
		videoPlayer.Stop();
		videoPlayer.clip = null;
	}

	public void ReleaseVideo()
	{
		videoRenderImage.color = Color.clear;
		videoPlayer.targetTexture.Release();
	}
}
