using System;
using System.Collections.Generic;

namespace ExitGames.Client.Photon
{
	internal class InvocationCache
	{
		private class CachedOperation
		{
			public int InvocationId
			{
				get;
				set;
			}

			public Action Action
			{
				get;
				set;
			}
		}

		private readonly LinkedList<CachedOperation> cache = new LinkedList<CachedOperation>();

		private int nextInvocationId = 1;

		public int NextInvocationId
		{
			get
			{
				return this.nextInvocationId;
			}
		}

		public int Count
		{
			get
			{
				return this.cache.Count;
			}
		}

		public void Reset()
		{
			lock (this.cache)
			{
				this.nextInvocationId = 1;
				this.cache.Clear();
			}
		}

		public void Invoke(int invocationId, Action action)
		{
			lock (this.cache)
			{
				if (invocationId >= this.nextInvocationId)
				{
					if (invocationId == this.nextInvocationId)
					{
						this.nextInvocationId++;
						action();
						if (this.cache.Count > 0)
						{
							LinkedListNode<CachedOperation> i = this.cache.First;
							while (i != null && i.Value.InvocationId == this.nextInvocationId)
							{
								this.nextInvocationId++;
								i.Value.Action();
								i = i.Next;
								this.cache.RemoveFirst();
							}
						}
					}
					else
					{
						CachedOperation cachedOperation = new CachedOperation();
						cachedOperation.InvocationId = invocationId;
						cachedOperation.Action = action;
						CachedOperation op = cachedOperation;
						if (this.cache.Count == 0)
						{
							this.cache.AddLast(op);
						}
						else
						{
							for (LinkedListNode<CachedOperation> node = this.cache.First; node != null; node = node.Next)
							{
								if (node.Value.InvocationId > invocationId)
								{
									this.cache.AddBefore(node, op);
									return;
								}
							}
							this.cache.AddLast(op);
						}
					}
				}
			}
		}
	}
}
