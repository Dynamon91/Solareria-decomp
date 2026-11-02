using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class SplashSceneController : MonoBehaviour
{
	[SerializeField]
	private Image doodalLogo;

	[SerializeField]
	private Image shinsegaeLogo;

	[SerializeField]
	private Image fmodLogo;

	[SerializeField]
	private TextMeshProUGUI gamerTagText;

	private void Awake()
	{
		SetResolution();
		doodalLogo.color = GraphicHelper.TransparentColor;
		shinsegaeLogo.color = GraphicHelper.TransparentColor;
		fmodLogo.color = GraphicHelper.TransparentColor;
		gamerTagText.text = "";
		UnityEngine.Cursor.lockState = CursorLockMode.Confined;
	}

	private void Start()
	{
		SplashAsync().Forget();
	}

	private async UniTask SplashAsync()
	{
		Preload();
		UniTask uniTask = CheckGameDataAsync();
		UniTask uniTask2 = LogoFadeInOutAsync();
		await UniTask.WhenAll(uniTask2, uniTask);
		Game.Instance.UIManager.FadeCanvas.FadeOut(0f).Forget();
		Game.Instance.LoadTitleAsync().Forget();
	}

	private async UniTask LogoFadeInOutAsync()
	{
		await UniTaskExtension.WaitForSeconds(1f);
		await doodalLogo.DOAlpha(1f, 0.5f).WaitAsync();
		await UniTaskExtension.WaitForSeconds(1f);
		await doodalLogo.DOAlpha(0f, 0.5f).WaitAsync();
		await shinsegaeLogo.DOAlpha(1f, 0.5f).WaitAsync();
		await UniTaskExtension.WaitForSeconds(1f);
		await shinsegaeLogo.DOAlpha(0f, 0.5f).WaitAsync();
		await fmodLogo.DOAlpha(1f, 0.5f).WaitAsync();
		await UniTaskExtension.WaitForSeconds(1f);
		await fmodLogo.DOAlpha(0f, 0.5f).WaitAsync();
	}

	private async UniTask CheckGameDataAsync()
	{
		gamerTagText.text = SteamFriends.GetPersonaName();
		Game.LoadAndCacheGameData();
	}

	private static void Preload()
	{
		AddressableHelper.PreloadAssetAsync("Assets/Prefabs/Player.prefab").Forget();
		AddressableHelper.PreloadAssetAsync("Game").Forget();
	}

	private static void SetResolution()
	{
		foreach (Vector2Int resolution in GraphicHelper.Resolutions)
		{
			int width = Screen.width;
			int height = Screen.height;
			if (resolution.x >= width || resolution.y >= height)
			{
				Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.ExclusiveFullScreen);
				break;
			}
		}
	}
}
