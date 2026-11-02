using System.Collections.Generic;
using System.Linq;
using Library;
using PathCreation;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class PathMoveBehaviour : PlayableBehaviour
{
	public AnimationCurve Curve;

	private PathCreator path;

	private PathFlipper pathFlipper;

	private bool isInitialized;

	private Transform transform;

	private List<TrailRenderer> trails;

	private float distanceTravelled;

	public PathCreator Path
	{
		set
		{
			path = value;
			pathFlipper = path.GetComponent<PathFlipper>();
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		transform = playerData as Transform;
		if (!(transform == null) && !(path == null) && Curve != null)
		{
			InitializeIfNecessary(info);
			float time = (float)(playable.GetTime() / playable.GetDuration());
			float length = path.path.length;
			distanceTravelled = length * Curve.Evaluate(time);
			transform.position = path.path.GetPointAtDistance(distanceTravelled, EndOfPathInstruction.Stop);
			transform.rotation = path.path.GetRotationAtDistance(distanceTravelled, EndOfPathInstruction.Stop);
		}
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		InitializeIfNecessary(info);
		pathFlipper.FlipPathTo(transform.lossyScale.x > 0f);
		distanceTravelled = 0f;
		transform.position = path.path.GetPointAtDistance(0f, EndOfPathInstruction.Stop);
		transform.rotation = path.path.GetRotationAtDistance(0f, EndOfPathInstruction.Stop);
		if (trails == null)
		{
			return;
		}
		foreach (TrailRenderer trail in trails)
		{
			trail.Clear();
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (!(transform == null))
		{
			transform.position = path.path.GetPointAtDistance(0f, EndOfPathInstruction.Stop);
			transform.rotation = path.path.GetRotationAtDistance(0f, EndOfPathInstruction.Stop);
		}
	}

	public override void OnGraphStop(Playable playable)
	{
		if (!(transform == null))
		{
			transform.position = path.path.GetPointAtDistance(0f, EndOfPathInstruction.Stop);
			transform.rotation = path.path.GetRotationAtDistance(0f, EndOfPathInstruction.Stop);
		}
	}

	private void InitializeIfNecessary(FrameData info)
	{
		if (!isInitialized)
		{
			if (BasicExtension.IsNull(transform))
			{
				transform = info.output.GetUserData() as Transform;
			}
			trails = transform.GetComponentsInChildren<TrailRenderer>().ToList();
			isInitialized = true;
		}
	}
}
