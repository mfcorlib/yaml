using System.Collections.Generic;
using System.Linq;

namespace Linearstar.Core.Yaml
{
	public class YMapping : YCollection
	{
		readonly List<YKeyValuePair> children = new List<YKeyValuePair>();

		public override YNode FirstNode => children.FirstOrDefault();
		public override YNode LastNode => children.LastOrDefault();

		public YNode this[YScalar key]
		{
			get => children.FirstOrDefault(i => i.Key.Equals(key))?.Value;
			set
			{
				if (children.FirstOrDefault(i => i.Key.Equals(key)) is YKeyValuePair kvp)
					kvp.Value = value;
				else
					children.Add(new YKeyValuePair(key, value));
			}
		}

		public YMapping(params object[] content)
			: base(content)
		{
		}

		internal static new YMapping Parse(Tokenizer tokenizer)
		{
			switch (tokenizer.Current.Kind)
			{
				case TokenKind.Indent when tokenizer.Next.Value.Kind == TokenKind.MappingKey:
					{
						var items = new List<YNode>();

						tokenizer.MoveNext();

						while (tokenizer.Current.Kind == TokenKind.MappingKey)
							items.Add(YKeyValuePair.Parse(tokenizer));

						if (tokenizer.Current.Kind == TokenKind.Unindent)
							tokenizer.MoveNext();

						return new YMapping(items.ToArray());
					}
				case TokenKind.Indent when tokenizer.Next.Next?.Value.Kind == TokenKind.MappingValue:
					{
						var items = new List<YNode>();

						tokenizer.MoveNext();

						while (tokenizer.Current.Kind != TokenKind.Unindent && tokenizer.Current.Kind != TokenKind.Eof)
							items.Add(YKeyValuePair.Parse(tokenizer));

						if (tokenizer.Current.Kind == TokenKind.Unindent)
							tokenizer.MoveNext();

						return new YMapping(items.ToArray());
					}
				case TokenKind.MappingBegin:
					{
						var items = new List<YKeyValuePair>();

						tokenizer.MoveNext();

						do
							if (tokenizer.Current.Kind == TokenKind.MappingEnd)
								break;
							else
								items.Add(YKeyValuePair.Parse(tokenizer));
						while (tokenizer.Current.Kind == TokenKind.ItemDelimiter && tokenizer.MoveNext());

						if (tokenizer.Current.Kind != TokenKind.MappingEnd)
							throw ParseException.UnexpectedToken(tokenizer, TokenKind.MappingEnd);

						tokenizer.MoveNext();

						return new YMapping(items.ToArray()) { Style = YNodeStyle.Flow };
					}
				default:
					return null;
			}
		}

		public override void Add(params object[] content) =>
			children.AddRange(Flattern(content).OfType<YKeyValuePair>());

		public override void AddFirst(params object[] content) =>
			children.InsertRange(0, Flattern(content).OfType<YKeyValuePair>());

		public override void RemoveNodes() =>
			children.Clear();

		protected internal override YNode GetPreviousNode(YNode node) =>
			node == FirstNode ? null :
				node is YKeyValuePair pair ? children[children.IndexOf(pair) - 1] : null;

		protected internal override YNode GetNextNode(YNode node) =>
			node == LastNode ? null :
				node is YKeyValuePair pair ? children[children.IndexOf(pair) + 1] : null;

		protected internal override void RemoveChild(YNode node)
		{
			if (node is YKeyValuePair pair)
				children.Remove(pair);
		}

		public override IEnumerator<YNode> GetEnumerator() =>
			children.GetEnumerator();

		public override string ToString(YNodeStyle style) =>
			style == YNodeStyle.Block
				? $"!!map {{\n{string.Join("\n", this.Select(i => AddIndent(i.ToString() + ",")))}\n}}"
				: $"!!map {{ {string.Join(" ", this.Select(i => i.ToString(style) + ","))} }}";

		public override string ToYamlString(YNodeStyle style) =>
			style == YNodeStyle.Block
				? string.Join("\n", this.Select(i => i.ToYamlString(style)))
				: this.Any() ? $"{{ {string.Join(", ", this.Select(i => i.ToYamlString(style)))} }}" : "{}";
	}
}
