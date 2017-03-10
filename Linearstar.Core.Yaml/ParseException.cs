using System;
using System.Linq;

namespace Linearstar.Core.Yaml
{
	public class ParseException : Exception
	{
		public string Content { get; }
		public int Index { get; }
		public int Line { get; }
		public int Column { get; }

		public ParseException(string content, int index, string message)
			: this(content, index, GetLine(content, index), GetColumn(content, index), message)
		{
		}

		public ParseException(string content, int index, int line, int column, string message)
			: base($"{message} at line {line + 1} column {column + 1}")
		{
			Content = Content;
			Index = index;
			Line = Line;
			Column = Column;
		}

		static int GetColumn(string content, int index)
		{
			var lineStart = index > 0 ? content.LastIndexOfAny(new[] { '\r', '\n' }, index - 1) : -1;

			return (lineStart == -1 ? index : index - lineStart - 1);
		}

		static int GetLine(string content, int index) =>
			content.Substring(0, index).Length - content.Substring(0, index).Replace("\n", null).Length;

		internal static ParseException TokenNotAllowed(Scanner scanner) =>
			new ParseException(scanner.Content, scanner.Index, $"{GetTokenName(scanner.Current)} not allowed in this context");

		internal static ParseException UnexpectedToken(Scanner scanner, params char[] expected) =>
			UnexpectedToken(scanner, GetTokenNames(expected));

		internal static ParseException UnexpectedToken(Scanner scanner, string expected) =>
			new ParseException(scanner.Content, scanner.Index, $"unexpected {GetTokenName(scanner.Current)}, {expected} expected");

		internal static ParseException UnexpectedToken(Tokenizer tokenizer, TokenKind expected) =>
			new ParseException(tokenizer.Scanner.Content, tokenizer.Current.Index, $"unexpected {tokenizer.Current.Kind}, {expected} expected");

		static string GetTokenNames(params char[] current) =>
			string.Join(" or ", current.Select(GetTokenName));

		static string GetTokenName(char current)
		{
			switch (current)
			{
				case '\0': return "EOF";
				case '\n': return "NEWLINE";
				case ' ': return "SPACE";
				default: return current.ToString();
			}
		}
	}
}
