using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ScreenController : MonoBehaviour
{
	[Serializable]
	public class ScreenBoard
	{
		[SerializeField]
		private Image screenImage;

		public void Init()
		{
			screenImage.gameObject.SetActive(value: false);
			screenImage.color = Color.black;
		}

		public Tween ChangeScreenColor(Color color, float duration)
		{
			screenImage.gameObject.SetActive(value: true);
			return screenImage.DOColor(color, duration).OnComplete(delegate
			{
				screenImage.gameObject.SetActive(color != Color.clear);
			});
		}
	}

	[SerializeField]
	private ScreenBoard screenBoard;

	public void Initialize()
	{
		base.gameObject.SetActive(value: true);
		screenBoard.Init();
	}

	public void ResetToDeactivated()
	{
	}

	public Tween ScreenColor(Color color, float duration)
	{
		return screenBoard.ChangeScreenColor(color, duration);
	}
}
