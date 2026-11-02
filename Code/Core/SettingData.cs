using System;
using System.Text;
using SPR.Library;
using Steamworks;
using UnityEngine;

[Serializable]
public class SettingData : IUserData
{
	[SerializeField]
	private Language language;

	[SerializeField]
	private int masterVol;

	[SerializeField]
	private int musicVol;

	[SerializeField]
	private int soundVol;

	[SerializeField]
	private int voiceVol;

	[SerializeField]
	private FullScreenMode fullscreen;

	[SerializeField]
	private Vector2 resolution;

	[SerializeField]
	private RewiredData rewiredData;

	[SerializeField]
	private JoystickData joystickData;

	[SerializeField]
	private bool vSync;

	[SerializeField]
	private bool damageEffect;

	[SerializeField]
	private bool pyronActionVignette;

	[SerializeField]
	private bool healthVignette;

	[SerializeField]
	private bool combatCamera;

	public Language Language
	{
		get
		{
			return language;
		}
		set
		{
			if (!object.Equals(language, value))
			{
				language = value;
			}
		}
	}

	public int MasterVol
	{
		get
		{
			return masterVol;
		}
		set
		{
			masterVol = value;
		}
	}

	public int SoundVol
	{
		get
		{
			return soundVol;
		}
		set
		{
			soundVol = value;
		}
	}

	public int MusicVol
	{
		get
		{
			return musicVol;
		}
		set
		{
			musicVol = value;
		}
	}

	public int VoiceVol
	{
		get
		{
			return voiceVol;
		}
		set
		{
			voiceVol = value;
		}
	}

	public FullScreenMode Fullscreen
	{
		get
		{
			return fullscreen;
		}
		set
		{
			fullscreen = value;
		}
	}

	public Vector2 Resolution
	{
		get
		{
			return resolution;
		}
		set
		{
			resolution = value;
		}
	}

	public RewiredData RewiredData => rewiredData;

	public JoystickData JoystickData => joystickData;

	public bool VSync
	{
		get
		{
			return vSync;
		}
		set
		{
			QualitySettings.vSyncCount = (value ? 1 : 0);
			vSync = value;
		}
	}

	public bool DamageEffect
	{
		get
		{
			return damageEffect;
		}
		set
		{
			damageEffect = value;
		}
	}

	public bool PyronActionVignette
	{
		get
		{
			return pyronActionVignette;
		}
		set
		{
			pyronActionVignette = value;
		}
	}

	public bool HealthVignette
	{
		get
		{
			return healthVignette;
		}
		set
		{
			healthVignette = value;
		}
	}

	public bool CombatCamera
	{
		get
		{
			return combatCamera;
		}
		set
		{
			combatCamera = value;
		}
	}

	public SettingData()
	{
		if (language == Language.None)
		{
			language = GetDefaultLanguage();
		}
		masterVol = 10;
		musicVol = 10;
		soundVol = 10;
		voiceVol = 10;
		fullscreen = FullScreenMode.FullScreenWindow;
		resolution = new Vector2Int(1920, 1080);
		rewiredData = new RewiredData();
		joystickData = new JoystickData();
		if (SteamUtils.IsSteamRunningOnSteamDeck())
		{
			vSync = false;
		}
		else
		{
			vSync = true;
		}
		damageEffect = true;
		pyronActionVignette = true;
		healthVignette = true;
		combatCamera = true;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append($"{language}, Master:{MasterVol}, Sound:{soundVol}, Music:{musicVol}, Voice:{voiceVol}");
		return stringBuilder.ToString();
	}

	public void RefineDataOnInitialize()
	{
		rewiredData.RefineDataOnInitialize();
	}

	public bool IsValid()
	{
		if (rewiredData != null)
		{
			return rewiredData.IsValid();
		}
		return false;
	}

	public void SetNativeResolution()
	{
		foreach (Vector2Int resolution in GraphicHelper.Resolutions)
		{
			int width = Screen.width;
			_ = Screen.height;
			if (resolution.x >= width)
			{
				Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.ExclusiveFullScreen);
				this.resolution = new Vector2(resolution.x, resolution.y);
				return;
			}
		}
		this.resolution = new Vector2(1920f, 1080f);
	}

	public void OnSaveLoaded()
	{
		QualitySettings.vSyncCount = (VSync ? 1 : 0);
	}

	private Language GetDefaultLanguage()
	{
		return Application.systemLanguage switch
		{
			SystemLanguage.Korean => Language.Korean, 
			SystemLanguage.Japanese => Language.Japanese, 
			SystemLanguage.ChineseSimplified => Language.SimplifiedChinese, 
			SystemLanguage.ChineseTraditional => Language.TraditionalChinese, 
			SystemLanguage.German => Language.German, 
			SystemLanguage.Spanish => Language.Spanish, 
			SystemLanguage.Portuguese => Language.Portuguese, 
			SystemLanguage.French => Language.French, 
			_ => Language.English, 
		};
	}
}
