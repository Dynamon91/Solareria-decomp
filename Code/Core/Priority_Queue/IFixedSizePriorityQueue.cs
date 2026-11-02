using System.Collections;
using System.Collections.Generic;

namespace Priority_Queue;

internal interface IFixedSizePriorityQueue<TItem, in TPriority> : IPriorityQueue<TItem, TPriority>, IEnumerable<TItem>, IEnumerable
{
	int MaxSize { get; }

	void Resize(int maxNodes);

	void ResetNode(TItem node);
}
