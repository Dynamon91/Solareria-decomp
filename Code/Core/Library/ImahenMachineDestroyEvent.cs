using SPR;

namespace Library;

public class ImahenMachineDestroyEvent : Event
{
	public ImahenSection ImahenSection { get; private set; }

	public ImahenMachineDestroyEvent(ImahenSection section)
	{
		ImahenSection = section;
	}
}
