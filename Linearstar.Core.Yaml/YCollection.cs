using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Linearstar.Core.Yaml
{
	public abstract class YCollection : YNode, IEnumerable<YNode>
	{
		public abstract YNode FirstNode { get; }
		public abstract YNode LastNode { get; }

		public YCollection(params object[] content) =>
			Add(content);

		public IEnumerable<YNode> Descendants()
		{
			foreach (var i in this)
			{
				yield return i;

				if (i is YCollection container)
					foreach (var j in container.Descendants())
						yield return j;
			}
		}
		protected internal abstract YNode GetPreviousNode(YNode node);
		protected internal abstract YNode GetNextNode(YNode node);
		public abstract void Add(params object[] content);
		public abstract void AddFirst(params object[] content);
		protected internal abstract void RemoveChild(YNode node);
		public abstract void RemoveNodes();

		public virtual void ReplaceNodes(params object[] content)
		{
			RemoveNodes();
			Add(content);
		}

		public abstract IEnumerator<YNode> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		protected static IEnumerable<object> Flattern(IEnumerable<object> objects) =>
			objects.SelectMany(i =>
				i is YNode ? new[] { i } :
				i is IEnumerable<object> objs ? Flattern(objs) :
				i is IEnumerable enumerable ? Flattern(enumerable.Cast<object>()) : new[] { i });
	}
}
