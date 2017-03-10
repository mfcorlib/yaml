using System.Collections.Generic;
using System.Linq;

namespace Linearstar.Core.Yaml
{
	public class YDocument : YCollection
	{
		readonly List<YNode> children = new List<YNode>();

		public override YNode FirstNode => children.FirstOrDefault();
		public override YNode LastNode => children.LastOrDefault();

		public YDocument(params object[] content)
			: base(content)
		{
		}

		internal static new YDocument Parse(Tokenizer tokenizer)
		{
			if (tokenizer.Current.Kind != TokenKind.Document) return null;

			tokenizer.MoveNext();

			var items = new List<YNode>();

			while (tokenizer.Current.Kind != TokenKind.Document && tokenizer.Current.Kind != TokenKind.Eof)
				items.Add(YNode.Parse(tokenizer));

			return new YDocument(items.ToArray());
		}

		public override void Add(params object[] content) =>
			children.AddRange(Flattern(content).Select(ToNode).Where(i => i != null));

		public override void AddFirst(params object[] content) =>
			children.InsertRange(0, Flattern(content).Select(ToNode).Where(i => i != null));

		protected internal override YNode GetPreviousNode(YNode node) =>
			node == FirstNode ? null : children[children.IndexOf(node) - 1];

		protected internal override YNode GetNextNode(YNode node) =>
			node == LastNode ? null : children[children.IndexOf(node) + 1];

		protected internal override void RemoveChild(YNode node) =>
			children.Remove(node);

		public override void RemoveNodes() =>
			children.Clear();

		public override IEnumerator<YNode> GetEnumerator() =>
			children.GetEnumerator();

		public override string ToString(YNodeStyle style) =>
			"---\n" + string.Join("\n", this.Select(i => i.ToString()));

		public override string ToYamlString(YNodeStyle style) =>
			"---\n" + string.Join("\n", this.Select(i => i.ToYamlString()));

	}
}
