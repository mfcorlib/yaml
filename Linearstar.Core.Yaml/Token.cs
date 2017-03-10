namespace Linearstar.Core.Yaml
{
	class Token
	{
		public string Content { get; }
		public TokenKind Kind { get; }
		public int Index { get; }
		public int Length { get; }
		public string Value { get; set; }

		public Token(Scanner scanner, TokenKind kind, int? index = null, int? length = null)
			: this(scanner, kind, null, index, length)
		{
		}

		public Token(Scanner scanner, TokenKind kind, string value, int? index = null, int? length = null)
		{
			Content = scanner.Content;
			Kind = kind;
			Value = value;
			Index = index ?? scanner.Index;
			Length = length ?? value?.Length ?? 1;
		}

		public override string ToString() =>
			$"({Kind}) {Content.Substring(Index, Length)}";
	}
}
