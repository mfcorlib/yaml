using System.Collections.Generic;
using System.Linq;

namespace Linearstar.Core.Yaml
{
	public class YSequence : YCollection
	{
		readonly List<YNode> children = new List<YNode>();

		public override YNode FirstNode => children.FirstOrDefault();
		public override YNode LastNode => children.LastOrDefault();

		public YSequence(params object[] content)
			: base(content)
		{
		}

		internal static new YSequence Parse(Tokenizer tokenizer)
		{
			switch (tokenizer.Current.Kind)
			{
				case TokenKind.Indent when tokenizer.Next.Value.Kind == TokenKind.SequenceValue:
					{
						var items = new List<YNode>();

						tokenizer.MoveNext();

						while (tokenizer.Current.Kind != TokenKind.Unindent && tokenizer.Current.Kind != TokenKind.Eof)
						{
							if (tokenizer.Current.Kind != TokenKind.SequenceValue)
								throw ParseException.UnexpectedToken(tokenizer, TokenKind.SequenceValue);

							tokenizer.MoveNext();
							items.Add(YNode.Parse(tokenizer));
						}

						if (tokenizer.Current.Kind == TokenKind.Unindent)
							tokenizer.MoveNext();

						return new YSequence(items.ToArray());
					}
				case TokenKind.SequenceBegin:
					{
						var items = new List<YNode>();

						tokenizer.MoveNext();

						do
							if (tokenizer.Current.Kind == TokenKind.SequenceEnd)
								break;
							else
								items.Add(YNode.Parse(tokenizer));
						while (tokenizer.Current.Kind == TokenKind.ItemDelimiter && tokenizer.MoveNext());

						if (tokenizer.Current.Kind != TokenKind.SequenceEnd)
							throw ParseException.UnexpectedToken(tokenizer, TokenKind.SequenceEnd);

						tokenizer.MoveNext();

						return new YSequence(items.ToArray()) { Style = YNodeStyle.Flow };
					}
				default:
					return null;
			}
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
			style == YNodeStyle.Block
				? $"!!seq [\n{string.Join("\n", this.Select(i => AddIndent(i.ToString() + ",")))}\n]"
				: $"!!seq [ {string.Join(" ", this.Select(i => i.ToString(style) + ","))} ]";

		public override string ToYamlString(YNodeStyle style) =>
			style == YNodeStyle.Block
				? string.Join("\n", this.Select(i =>
				{
					var rt = i.ToYamlString();

					if (i.Style == YNodeStyle.Block && i is YSequence)
						rt = "\n" + AddIndent(rt);
					else
						rt = AddIndent(rt).Substring(2);

					return "- " + rt;
				}))
				: this.Any() ? $"[ {string.Join(", ", this.Select(i => i.ToYamlString(style)))} ]" : "[]";
	}
}
