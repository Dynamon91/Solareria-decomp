using System.Collections.Generic;
using Library;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SPR.Field;

public class Platform : MonoBehaviour, IDamageable, IGround
{
	[SerializeField]
	private SoundManager.GroundMaterialType groundMaterial;

	private Tilemap tilemap;

	private GroundMaterialDivider gmd;

	public static readonly HashSet<string> IgnoreBake = new HashSet<string> { "SolinLab_98" };

	private Tilemap Tilemap => tilemap ?? (tilemap = GetComponent<Tilemap>());

	public PathFinder PathFinder { get; private set; }

	public SoundManager.GroundMaterialType GroundMaterial
	{
		get
		{
			if (!gmd.IsNull())
			{
				return gmd.GetMaterialType(groundMaterial);
			}
			return groundMaterial;
		}
	}

	public HitType HitType => HitType.Platform;

	public Vector2 VisualCenter => Vector2.zero;

	private void Awake()
	{
		PathFinder = new PathFinder(Tilemap);
		gmd = GetComponent<GroundMaterialDivider>();
		if (!IgnoreBake.Contains(SceneManager.GetActiveScene().name))
		{
			PathFinder.BakeGraph(Tilemap);
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		return false;
	}
}
