using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SPR;

[CreateAssetMenu(fileName = "New LocaleFontSet", menuName = "Scriptable Object/LocaleFontSet")]
public class LocaleFontSet : ScriptableObject
{
	[SerializeField]
	private Font fallBackFont;

	[SerializeField]
	private TMP_FontAsset fallBackTMPFont;

	[SerializeField]
	private List<FontSet> fontSetList = new List<FontSet>();

	public Font GetCurrentLocaleFont(TextType textType, Language language)
	{
		foreach (FontSet fontSet in fontSetList)
		{
			if (fontSet.TextType == textType)
			{
				int num = (int)(language - 1);
				if (num < fontSet.Fonts.Count)
				{
					return fontSet.Fonts[num];
				}
			}
		}
		return fallBackFont;
	}

	public TMP_FontAsset GetCurrentLocaleTMPFont(TextType textType, Language language)
	{
		foreach (FontSet fontSet in fontSetList)
		{
			if (fontSet.TextType == textType)
			{
				int num = (int)(language - 1);
				if (num < fontSet.TMPFonts.Count)
				{
					return fontSet.TMPFonts[num];
				}
			}
		}
		return fallBackTMPFont;
	}
}
