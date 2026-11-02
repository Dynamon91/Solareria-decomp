using System;
using System.Collections;
using System.Collections.Generic;

namespace Priority_Queue;

public sealed class GenericPriorityQueue<TItem, TPriority> : IFixedSizePriorityQueue<TItem, TPriority>, IPriorityQueue<TItem, TPriority>, IEnumerable<TItem>, IEnumerable where TItem : GenericPriorityQueueNode<TPriority>
{
	private int _numNodes;

	private TItem[] _nodes;

	private long _numNodesEverEnqueued;

	private readonly Comparison<TPriority> _comparer;

	public int Count => _numNodes;

	public int MaxSize => _nodes.Length - 1;

	public TItem First => _nodes[1];

	public GenericPriorityQueue(int maxNodes)
		: this(maxNodes, (IComparer<TPriority>)Comparer<TPriority>.Default)
	{
	}

	public GenericPriorityQueue(int maxNodes, IComparer<TPriority> comparer)
		: this(maxNodes, (Comparison<TPriority>)comparer.Compare)
	{
	}

	public GenericPriorityQueue(int maxNodes, Comparison<TPriority> comparer)
	{
		_numNodes = 0;
		_nodes = new TItem[maxNodes + 1];
		_numNodesEverEnqueued = 0L;
		_comparer = comparer;
	}

	public void Clear()
	{
		Array.Clear(_nodes, 1, _numNodes);
		_numNodes = 0;
	}

	public bool Contains(TItem node)
	{
		return _nodes[node.QueueIndex] == node;
	}

	public void Enqueue(TItem node, TPriority priority)
	{
		node.Priority = priority;
		_numNodes++;
		_nodes[_numNodes] = node;
		node.QueueIndex = _numNodes;
		node.InsertionIndex = _numNodesEverEnqueued++;
		CascadeUp(node);
	}

	private void CascadeUp(TItem node)
	{
		if (node.QueueIndex <= 1)
		{
			return;
		}
		int num = node.QueueIndex >> 1;
		TItem val = _nodes[num];
		if (HasHigherPriority(val, node))
		{
			return;
		}
		_nodes[node.QueueIndex] = val;
		val.QueueIndex = node.QueueIndex;
		node.QueueIndex = num;
		while (num > 1)
		{
			num >>= 1;
			TItem val2 = _nodes[num];
			if (HasHigherPriority(val2, node))
			{
				break;
			}
			_nodes[node.QueueIndex] = val2;
			val2.QueueIndex = node.QueueIndex;
			node.QueueIndex = num;
		}
		_nodes[node.QueueIndex] = node;
	}

	private void CascadeDown(TItem node)
	{
		int queueIndex = node.QueueIndex;
		int num = 2 * queueIndex;
		if (num > _numNodes)
		{
			return;
		}
		int num2 = num + 1;
		TItem val = _nodes[num];
		if (HasHigherPriority(val, node))
		{
			if (num2 > _numNodes)
			{
				node.QueueIndex = num;
				val.QueueIndex = queueIndex;
				_nodes[queueIndex] = val;
				_nodes[num] = node;
				return;
			}
			TItem val2 = _nodes[num2];
			if (HasHigherPriority(val, val2))
			{
				val.QueueIndex = queueIndex;
				_nodes[queueIndex] = val;
				queueIndex = num;
			}
			else
			{
				val2.QueueIndex = queueIndex;
				_nodes[queueIndex] = val2;
				queueIndex = num2;
			}
		}
		else
		{
			if (num2 > _numNodes)
			{
				return;
			}
			TItem val3 = _nodes[num2];
			if (!HasHigherPriority(val3, node))
			{
				return;
			}
			val3.QueueIndex = queueIndex;
			_nodes[queueIndex] = val3;
			queueIndex = num2;
		}
		while (true)
		{
			num = 2 * queueIndex;
			if (num > _numNodes)
			{
				node.QueueIndex = queueIndex;
				_nodes[queueIndex] = node;
				return;
			}
			num2 = num + 1;
			val = _nodes[num];
			if (HasHigherPriority(val, node))
			{
				if (num2 > _numNodes)
				{
					node.QueueIndex = num;
					val.QueueIndex = queueIndex;
					_nodes[queueIndex] = val;
					_nodes[num] = node;
					return;
				}
				TItem val4 = _nodes[num2];
				if (HasHigherPriority(val, val4))
				{
					val.QueueIndex = queueIndex;
					_nodes[queueIndex] = val;
					queueIndex = num;
				}
				else
				{
					val4.QueueIndex = queueIndex;
					_nodes[queueIndex] = val4;
					queueIndex = num2;
				}
			}
			else
			{
				if (num2 > _numNodes)
				{
					node.QueueIndex = queueIndex;
					_nodes[queueIndex] = node;
					return;
				}
				TItem val5 = _nodes[num2];
				if (!HasHigherPriority(val5, node))
				{
					break;
				}
				val5.QueueIndex = queueIndex;
				_nodes[queueIndex] = val5;
				queueIndex = num2;
			}
		}
		node.QueueIndex = queueIndex;
		_nodes[queueIndex] = node;
	}

	private bool HasHigherPriority(TItem higher, TItem lower)
	{
		int num = _comparer(higher.Priority, lower.Priority);
		if (num >= 0)
		{
			if (num == 0)
			{
				return higher.InsertionIndex < lower.InsertionIndex;
			}
			return false;
		}
		return true;
	}

	public TItem Dequeue()
	{
		TItem result = _nodes[1];
		if (_numNodes == 1)
		{
			_nodes[1] = null;
			_numNodes = 0;
			return result;
		}
		TItem val = _nodes[_numNodes];
		_nodes[1] = val;
		val.QueueIndex = 1;
		_nodes[_numNodes] = null;
		_numNodes--;
		CascadeDown(val);
		return result;
	}

	public void Resize(int maxNodes)
	{
		TItem[] array = new TItem[maxNodes + 1];
		int num = Math.Min(maxNodes, _numNodes);
		Array.Copy(_nodes, array, num + 1);
		_nodes = array;
	}

	public void UpdatePriority(TItem node, TPriority priority)
	{
		node.Priority = priority;
		OnNodeUpdated(node);
	}

	private void OnNodeUpdated(TItem node)
	{
		int num = node.QueueIndex >> 1;
		if (num > 0 && HasHigherPriority(node, _nodes[num]))
		{
			CascadeUp(node);
		}
		else
		{
			CascadeDown(node);
		}
	}

	public void Remove(TItem node)
	{
		if (node.QueueIndex == _numNodes)
		{
			_nodes[_numNodes] = null;
			_numNodes--;
			return;
		}
		TItem val = _nodes[_numNodes];
		_nodes[node.QueueIndex] = val;
		val.QueueIndex = node.QueueIndex;
		_nodes[_numNodes] = null;
		_numNodes--;
		OnNodeUpdated(val);
	}

	public void ResetNode(TItem node)
	{
		node.QueueIndex = 0;
	}

	public IEnumerator<TItem> GetEnumerator()
	{
		for (int i = 1; i <= _numNodes; i++)
		{
			yield return _nodes[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public bool IsValidQueue()
	{
		for (int i = 1; i < _nodes.Length; i++)
		{
			if (_nodes[i] != null)
			{
				int num = 2 * i;
				if (num < _nodes.Length && _nodes[num] != null && HasHigherPriority(_nodes[num], _nodes[i]))
				{
					return false;
				}
				int num2 = num + 1;
				if (num2 < _nodes.Length && _nodes[num2] != null && HasHigherPriority(_nodes[num2], _nodes[i]))
				{
					return false;
				}
			}
		}
		return true;
	}
}
