using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linearstar.Core.Yaml
{
	class Tokenizer : IEnumerable<Token>
	{
		LinkedListNode<Token> currentNode;

		public Scanner Scanner { get; }
		public LinkedList<Token> Tokens { get; }
		public Token Current => currentNode.Value;
		public LinkedListNode<Token> Previous => currentNode.Previous;
		public LinkedListNode<Token> Next => currentNode.Next;

		public Tokenizer(Scanner scanner)
		{
			Scanner = scanner;
			Tokens = new LinkedList<Token>(Tokenize(scanner));
			currentNode = Tokens.First;
		}

		public bool MovePrevious() =>
			(currentNode = currentNode.Previous) != null;

		public bool MoveNext() =>
			(currentNode = currentNode.Next) != null;

		static IReadOnlyList<Token> Tokenize(Scanner scanner)
		{
			var tokens = new List<Token>();

			while (!scanner.IsEnd)
			{
				Read(scanner, tokens);

				if (tokens.LastOrDefault()?.Kind == TokenKind.Eof)
					break;
			}

			if (tokens.LastOrDefault()?.Kind == TokenKind.Eof)
				tokens.RemoveAt(tokens.Count - 1);

			while (scanner.CurrentIndent >= 0)
			{
				scanner.PopIndent();
				tokens.Add(new Token(scanner, TokenKind.Unindent, length: 0));
			}

			tokens.Add(new Token(scanner, TokenKind.Eof, length: 0));

			return tokens;
		}

		static void Read(Scanner scanner, List<Token> tokens)
		{
			scanner.SkipEmptyLines();
			scanner.SkipWhiteSpace();

			var indent = scanner.Column;

			if (SkipComment(scanner) && scanner.ReadLineBreak() != null)
			{
				scanner.SkipEmptyLines();
				indent = scanner.SkipWhiteSpace();
			}

			if (!scanner.IsFlowContent)
			{
				if (scanner.ReadLineBreak() != null)
				{
					scanner.MaybeSimpleKey = true;
					scanner.SkipEmptyLines();
					indent = scanner.SkipWhiteSpace();
				}

				if (scanner.Index > 0 && scanner.IsLineBreak(-1))
					while (indent < scanner.CurrentIndent)
					{
						scanner.PopIndent();
						tokens.Add(new Token(scanner, TokenKind.Unindent, length: 0));
					}

				if (indent == 0)
					switch (scanner.Current)
					{
						case '%':
							var begin = scanner.Index;

							while (scanner.CurrentIndent >= 0)
							{
								scanner.PopIndent();
								tokens.Add(new Token(scanner, TokenKind.Unindent, length: 0));
							}

							scanner.MaybeSimpleKey = false;
							tokens.Add(new Token(scanner, TokenKind.Directive, scanner.ReadUntilLineBreakOrEof().Substring(1), begin));

							return;
						default:
							if (!scanner.IsCurrent("---", "...") || !scanner.IsWhiteSpaceOrLineBreakOrEof(3))
								break;

							while (scanner.CurrentIndent >= 0)
							{
								scanner.PopIndent();
								tokens.Add(new Token(scanner, TokenKind.Unindent, length: 0));
							}

							scanner.MaybeSimpleKey = false;
							tokens.Add(NewToken(scanner.Current == '-' ? TokenKind.Document : TokenKind.Eof, 3));

							return;
					}
			}

			switch (scanner.Current)
			{
				case '\0':
					tokens.Add(new Token(scanner, TokenKind.Eof, length: 0));

					return;
				case '!':
					{
						var begin = scanner.Index++;
						var value = scanner.ReadUntilWhiteSpaceOrEof();

						scanner.MaybeSimpleKey = false;
						tokens.Add(new Token(scanner, TokenKind.Tag, value, begin, scanner.Index - begin));

						return;
					}
				case '[':
				case '{':
					{
						var kind = scanner.Current;

						scanner.FlowLevel++;
						scanner.MaybeSimpleKey = true;
						tokens.Add(NewToken(kind == '[' ? TokenKind.SequenceBegin : TokenKind.MappingBegin));

						return;
					}
				case ']':
				case '}':
					{
						var kind = scanner.Current;

						scanner.FlowLevel--;
						scanner.MaybeSimpleKey = false;
						tokens.Add(NewToken(kind == ']' ? TokenKind.SequenceEnd : TokenKind.MappingEnd));

						return;
					}
				case ',':
					scanner.MaybeSimpleKey = true;
					tokens.Add(NewToken(TokenKind.ItemDelimiter));

					return;
				case '-' when scanner.IsWhiteSpaceOrLineBreakOrEof(1):
					{
						if (scanner.IsFlowContent) throw ParseException.TokenNotAllowed(scanner);
						if (!scanner.MaybeSimpleKey) throw ParseException.TokenNotAllowed(scanner);

						var column = scanner.Column;

						if (column > scanner.CurrentIndent)
						{
							scanner.PushIndent(column);
							tokens.Add(new Token(scanner, TokenKind.Indent, scanner.Index - column, column));
						}

						var begin = scanner.Index++;

						scanner.SkipWhiteSpace();
						scanner.MaybeSimpleKey = true;
						tokens.Add(new Token(scanner, TokenKind.SequenceValue, begin));

						return;
					}
				case '?' when scanner.IsFlowContent || scanner.IsWhiteSpaceOrLineBreakOrEof(1):
					if (!scanner.IsFlowContent)
					{
						if (!scanner.MaybeSimpleKey) throw ParseException.TokenNotAllowed(scanner);

						if (indent > scanner.CurrentIndent)
						{
							scanner.PushIndent(indent);
							tokens.Add(new Token(scanner, TokenKind.Indent, scanner.Index - indent, indent));
						}
					}

					scanner.MaybeSimpleKey = !scanner.IsFlowContent;
					tokens.Add(NewToken(TokenKind.MappingKey));

					return;
				case ':' when scanner.IsFlowContent || scanner.IsWhiteSpaceOrLineBreakOrEof(1):
					if (!scanner.IsFlowContent && tokens.Any())
					{
						if (!scanner.MaybeSimpleKey) throw ParseException.TokenNotAllowed(scanner);

						indent = scanner.GetColumn(tokens.Last().Index);

						if (indent > scanner.CurrentIndent)
						{
							scanner.PushIndent(indent);
							tokens.Insert(tokens.Count - 1, new Token(scanner, TokenKind.Indent, scanner.Index - indent, indent));
						}
					}

					scanner.MaybeSimpleKey = !scanner.IsFlowContent;
					tokens.Add(NewToken(TokenKind.MappingValue));

					return;
				case '*':
				case '&':
					{
						var kind = scanner.Current;
						var begin = scanner.Index++;
						var value = scanner.ReadWhile(i => char.IsLetterOrDigit(i) || i == '-' || i == '_');

						if (string.IsNullOrEmpty(value)) throw ParseException.UnexpectedToken(scanner, "identifier");
						else if (!scanner.IsWhiteSpaceOrEof() && "]}?:,%@`".IndexOf(scanner.Current) == -1) throw ParseException.UnexpectedToken(scanner, ' ');

						scanner.MaybeSimpleKey = kind == '*';
						tokens.Add(new Token(scanner, kind == '*' ? TokenKind.Alias : TokenKind.Anchor, value, begin, scanner.Index - begin));

						return;
					}
				case '|' when !scanner.IsFlowContent:
				case '>' when !scanner.IsFlowContent:
					{
						var kind = scanner.Current;
						var begin = scanner.Index++;
						var trimEnd = false;
						var currentIndent = 0;
						var indentIndicator = 0;

						if (scanner.IsCurrent('+', '-'))
						{
							trimEnd = scanner.Current == '-';
							scanner.Index++;

							if (char.IsDigit(scanner.Current))
							{
								indentIndicator = scanner.Current - '0';

								if (indentIndicator == 0) throw ParseException.UnexpectedToken(scanner, "1-9");

								scanner.Index++;
							}
						}
						else if (char.IsDigit(scanner.Current))
						{
							indentIndicator = scanner.Current - '0';

							if (indentIndicator == 0) throw ParseException.UnexpectedToken(scanner, "1-9");

							scanner.Index++;

							if (scanner.IsCurrent('+', '-'))
							{
								trimEnd = scanner.Current == '-';
								scanner.Index++;
							}
						}

						scanner.SkipWhiteSpace();
						SkipComment(scanner);

						if (!scanner.IsLineBreakOrEof()) throw ParseException.UnexpectedToken(scanner, '\n');

						scanner.ReadLineBreak();
						scanner.SkipEmptyLines();

						if (indentIndicator > 0)
							currentIndent = scanner.CurrentIndent + indentIndicator;

						var sb = new StringBuilder();
						var wasLineBreakLine = false;

						while (!scanner.IsEnd)
						{
							var lineIndent = scanner.PeekWhiteSpace();

							if (currentIndent > 0 && lineIndent.Length < currentIndent) break;

							if (indentIndicator == 0)
							{
								indentIndicator = lineIndent.Length - currentIndent;
								currentIndent += indentIndicator;
							}

							if (scanner.IsLineBreak(currentIndent))
							{
								scanner.Index += currentIndent;

								if (scanner.IsLineBreak())
								{
									if (kind != '>' || wasLineBreakLine)
									{
										if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
											sb.Remove(sb.Length - 1, 1);

										sb.Append(scanner.ReadLineBreak());
									}

									scanner.SkipEmptyLines();
								}

								wasLineBreakLine = true;
							}
							else
							{
								sb.Append(scanner.ReadUntilLineBreakOrEof().Substring(currentIndent));
								wasLineBreakLine = false;

								if (scanner.IsLineBreak())
								{
									if (kind != '>' || lineIndent.Length > currentIndent)
										sb.Append(scanner.ReadLineBreak());
									else if (sb.Length > 0 && sb[sb.Length - 1] != ' ')
										sb.Append(" ");
								}
							}

							scanner.SkipEmptyLines();
						}

						var value = sb.ToString();

						if (trimEnd) value = value.TrimEnd();

						scanner.MaybeSimpleKey = false;
						tokens.Add(new Token(scanner, kind == '>' ? TokenKind.StringFolding : TokenKind.StringLiteral, value, begin, scanner.Index - begin));

						return;
					}
				case '"':
				case '\'':
					{
						var kind = scanner.Current;
						var begin = scanner.Index++;
						var sb = new StringBuilder();

						while (!scanner.IsEnd)
						{
							if (scanner.IsLineBreak())
							{
								scanner.ReadLineBreak();
								scanner.SkipWhiteSpace();

								if (scanner.IsLineBreak())
									do
									{
										sb.Append(scanner.ReadLineBreak());
										scanner.SkipWhiteSpace();
									}
									while (scanner.IsLineBreak());
								else if (sb.Length > 0 && sb[sb.Length - 1] != ' ')
									sb.Append(" ");
							}

							if (kind == '\'' && scanner.Current == kind && scanner.PeekChar(1) == kind)
								scanner.Index++;
							else if (scanner.Current == '\\')
							{
								sb.Append(scanner.Current);
								scanner.Index++;
							}
							else if (scanner.Current == kind)
							{
								scanner.Index++;

								break;
							}

							sb.Append(scanner.Current);
							scanner.Index++;
						}

						scanner.MaybeSimpleKey = true;
						tokens.Add(new Token(scanner, kind == '"' ? TokenKind.StringDouble : TokenKind.StringSingle, sb.ToString(), begin, scanner.Index - begin));

						return;
					}
				default:
					{
						if (scanner.IsCurrent('-', '?', ':', '|', '>', '%', '@', '`'))
							throw ParseException.UnexpectedToken(scanner, "Identifier");

						var begin = scanner.Index;
						var trailingSpaces = 0;
						var sb = new StringBuilder();

						while (!scanner.IsEnd)
						{
							if (scanner.Current == ':' && scanner.IsWhiteSpaceOrLineBreakOrEof(1) ||
								scanner.IsFlowContent && scanner.IsCurrent('?', ':', ',', '[', ']', '{', '}') ||
								scanner.Current == '#' ||
								scanner.IsCurrent("---", "...") && scanner.IsLineBreakOrEof(-1))
								break;

							if (scanner.IsLineBreak())
							{
								if (trailingSpaces > 0)
									sb.Remove(sb.Length - trailingSpaces, trailingSpaces);

								trailingSpaces = 0;
								scanner.MaybeSimpleKey = false;
								scanner.ReadLineBreak();

								var newIndent = scanner.SkipWhiteSpace();

								if (scanner.IsLineBreak())
								{
									sb.Append(scanner.SkipEmptyLines());
									newIndent = scanner.SkipWhiteSpace();
								}

								if (newIndent <= scanner.CurrentIndent)
									break;

								if (sb.Length > 0 && sb[sb.Length - 1] != ' ')
									sb.Append(" ");

								if (!scanner.IsFlowContent && scanner.CurrentIndent > 0 && newIndent < scanner.CurrentIndent)
									break;

								continue;
							}

							if (scanner.IsWhiteSpace())
								trailingSpaces++;
							else
								trailingSpaces = 0;

							sb.Append(scanner.Current);
							scanner.Index++;
						}

						scanner.MaybeSimpleKey = true;
						tokens.Add(new Token(scanner, TokenKind.StringPlain, sb.ToString().TrimEnd(), begin, scanner.Index - begin));

						return;
					}
			}

			Token NewToken(TokenKind kind, int? length = null)
			{
				var rt = new Token(scanner, kind, length: length);

				scanner.Index += rt.Length;

				return rt;
			}
		}

		static bool SkipComment(Scanner scanner)
		{
			if (scanner.Current != '#') return false;

			scanner.ReadUntilLineBreakOrEof();

			return true;
		}

		public IEnumerator<Token> GetEnumerator()
		{
			do
				yield return Current;
			while (MoveNext());
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
