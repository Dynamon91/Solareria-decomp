using SPR.Field;

namespace Library;

public class MyosoSupportEvent : Event
{
	public MyosoEventState MyosoEventState { get; }

	public bool IsPyronAction { get; }

	public MyosoSupportEvent(MyosoEventState state, bool isPyronAction = false)
	{
		MyosoEventState = state;
		IsPyronAction = isPyronAction;
	}
}
