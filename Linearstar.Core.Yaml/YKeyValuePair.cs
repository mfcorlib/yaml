namespace Linearstar.Core.Yaml
{
	public class YKeyValuePair : YNode
	{
		public YNode Key { get; set; }
		public YNode Value { get; set; }

		public YKeyValuePair(YNode key, YNode value)
		{
			Key = key;
			Value = value;
		}

		internal static new YKeyValuePair Parse(Tokenizer tokenizer)
		{
			switch (tokenizer.Current.Kind)
			{
				case TokenKind.MappingKey:
					{
						tokenizer.MoveNext();

						var key = YNode.Parse(tokenizer);

						if (tokenizer.Current.Kind != TokenKind.MappingValue)
							return new YKeyValuePair(key, new YScalar(null));

						tokenizer.MoveNext();

						var value = YNode.Parse(tokenizer);

						return new YKeyValuePair(key, value);
					}
				default:
					{
						var key = YNode.Parse(tokenizer);

						if (tokenizer.Current.Kind != TokenKind.MappingValue)
							throw ParseException.UnexpectedToken(tokenizer, TokenKind.MappingValue);

						tokenizer.MoveNext();

						var value = YNode.Parse(tokenizer);

						return new YKeyValuePair(key, value);
					}
			}
		}

		public override string ToString(YNodeStyle style) =>
			style == YNodeStyle.Block
				? $"? {Key}\n: {Value}"
				: $"? {Key.ToString(style)} : {Value.ToString(style)}";

		public override string ToYamlString(YNodeStyle style) =>
			style == YNodeStyle.Block
				? Key.ToYamlString(YNodeStyle.Flow) + ": " + (Value.Style == YNodeStyle.Block && Value is YCollection
					? "\n" + AddIndent(Value.ToYamlString())
					: AddIndent(Value.ToYamlString()).Substring(2))
				: Key.ToYamlString(YNodeStyle.Flow) + ": " + Value.ToYamlString(YNodeStyle.Flow);
	}
}
