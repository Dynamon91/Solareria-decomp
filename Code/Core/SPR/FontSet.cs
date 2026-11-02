using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SPR;

[Serializable]
public struct FontSet
{
	[SerializeField]
	private TextType textType;

	[SerializeField]
	private List<Font> fonts;

	[SerializeField]
	private List<TMP_FontAsset> tmpFonts;

	public TextType TextType => textType;

	public IReadOnlyList<Font> Fonts => fonts;

	public IReadOnlyList<TMP_FontAsset> TMPFonts => tmpFonts;
}
