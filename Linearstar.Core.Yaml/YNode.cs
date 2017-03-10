using System.Collections.Generic;
using System.Linq;

namespace Linearstar.Core.Yaml
{
	public abstract class YNode
	{
		public YCollection Parent { get; }
		public YNodeStyle Style { get; set; }
		public YNode PreviousNode => Parent?.GetPreviousNode(this);
		public YNode NextNode => Parent?.GetNextNode(this);

		public static IEnumerable<YNode> Parse(string content)
		{
			var tokenizer = new Tokenizer(new Scanner(content));

			while (tokenizer.Current.Kind != TokenKind.Eof && Parse(tokenizer) is YNode node)
				yield return node;

			if (tokenizer.Current.Kind != TokenKind.Eof) throw ParseException.UnexpectedToken(tokenizer, TokenKind.Eof);
		}

		internal static YNode Parse(Tokenizer tokenizer) =>
			YDocument.Parse(tokenizer) ??
			YMapping.Parse(tokenizer) ??
			YSequence.Parse(tokenizer) ??
			YScalar.Parse(tokenizer);

		public IEnumerable<YNode> NodesBeforeSelf() =>
			Parent.TakeWhile(i => i != this);

		public IEnumerable<YNode> NodesAfterSelf() =>
			Parent.SkipWhile(i => i != this).Skip(1);

		public void Remove() =>
			Parent.RemoveChild(this);

		public override string ToString() => ToString(Style);
		public abstract string ToString(YNodeStyle style);

		public string ToYamlString() => ToYamlString(Style);
		public abstract string ToYamlString(YNodeStyle style);

		protected static YNode ToNode(object content)
		{
			switch (content)
			{
				case null:
					return null;
				case YNode node:
					return node;
				default:
					return new YScalar(content);
			}
		}

		protected static string AddIndent(string str) =>
			"  " + str.Replace("\n", "\n  ");
	}
}
