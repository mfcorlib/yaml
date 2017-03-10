using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linearstar.Core.Yaml
{
	class Scanner
	{
		readonly Stack<int> indents = new Stack<int>();

		public string Content { get; }
		public int Index { get; set; }
		public int Column => GetColumn(Index);
		public int Line => GetLine(Index);
		public char Current => Index < Content.Length ? Content[Index] : '\0';
		public bool IsEnd => Current == '\0';

		public bool IsFlowContent => FlowLevel > 0;
		public int FlowLevel { get; set; }
		public int CurrentIndent => indents.Any() ? indents.Peek() : -1;
		public bool MaybeSimpleKey { get; set; } = true;

		public Scanner(string content) =>
			Content = content.Replace("\r\n", "\n");

		public int GetColumn(int index)
		{
			var lineStart = index > 0 ? Content.LastIndexOfAny(new[] { '\r', '\n' }, index - 1) : -1;

			return (lineStart == -1 ? index : index - lineStart - 1);
		}

		public int GetLine(int index) =>
			Content.Substring(0, index).Length - Content.Substring(0, index).Replace("\n", null).Length;

		public void PushIndent(int spaces) => indents.Push(spaces);
		public int PopIndent() => indents.Pop();

		public bool IsCurrent(params char[] any) => any.Any(i => Current == i);
		public bool IsCurrent(params string[] any) => any.Any(i => Peek(i.Length) == i);

		bool IsWhiteSpace(char c) => c == ' ' || (IsFlowContent || !MaybeSimpleKey) && c == '\t';
		public bool IsWhiteSpace(int offset = 0) => IsWhiteSpace(PeekChar(offset));

		bool IsWhiteSpaceOrEof(char c) => IsWhiteSpace(c) || c == '\0';
		public bool IsWhiteSpaceOrEof(int offset = 0) => IsWhiteSpaceOrEof(PeekChar(offset));

		bool IsLineBreak(char c) => c == '\r' || c == '\n';
		public bool IsLineBreak(int offset = 0) => IsLineBreak(PeekChar(offset));

		bool IsLineBreakOrEof(char c) => IsLineBreak(c) || c == '\0';
		public bool IsLineBreakOrEof(int offset = 0) => IsLineBreakOrEof(PeekChar(offset));

		bool IsWhiteSpaceOrLineBreakOrEof(char c) => IsWhiteSpace(c) || IsLineBreak(c) || c == '\0';
		public bool IsWhiteSpaceOrLineBreakOrEof(int offset = 0) => IsWhiteSpaceOrLineBreakOrEof(PeekChar(offset));

		public char PeekChar(int offset) =>
			0 <= Index + offset && Index + offset < Content.Length ? Content[Index + offset] : '\0';

		public string Peek(int length) =>
			new string(Enumerable.Range(Index, length)
								 .TakeWhile(i => i < Content.Length)
								 .Select(i => Content[i])
								 .ToArray());

		public string PeekWhile(Func<char, bool> predicate) =>
			new string(Enumerable.Range(Index, Content.Length - Index)
								 .Select(i => Content[i])
								 .TakeWhile(predicate)
								 .ToArray());

		public string PeekWhiteSpace() =>
			PeekWhile(IsWhiteSpace);

		public string PeekUntilWhiteSpace() =>
			PeekWhile(i => !IsWhiteSpace(i));

		string Read(string peek)
		{
			Index += peek.Length;

			return peek;
		}

		public string ReadWhile(Func<char, bool> predicate) =>
			Read(PeekWhile(predicate));

		public string ReadUntilWhiteSpaceOrEof()
		{
			var begin = Index;

			while (!IsWhiteSpaceOrEof()) Index++;

			return Content.Substring(begin, Index - begin);
		}

		public string ReadUntilLineBreakOrEof()
		{
			var begin = Index;

			while (!IsLineBreakOrEof()) Index++;

			return Content.Substring(begin, Index - begin);
		}

		public int SkipWhiteSpace()
		{
			var begin = Index;

			while (IsWhiteSpace()) Index++;

			return Index - begin;
		}

		public string SkipEmptyLines()
		{
			var sb = new StringBuilder();

			while (true)
			{
				var ws = PeekWhiteSpace();

				if (IsLineBreakOrEof(ws.Length))
				{
					Index += ws.Length;
					sb.Append(ReadLineBreak());
				}
				else
					break;
			}

			return sb.ToString();
		}

		public string ReadLineBreak()
		{
			if (IsCurrent("\r\n"))
			{
				Index += 2;

				return "\r\n";
			}
			else if (IsLineBreak())
			{
				var c = Current;

				Index++;

				return c.ToString();
			}

			return null;
		}

		public override string ToString()
		{
			var lineStart = Index > 0 ? Content.LastIndexOfAny(new[] { '\r', '\n' }, Index - 1) : -1;
			var lineEnd = Content.IndexOfAny(new[] { '\r', '\n' }, Index);

			if (lineStart == -1) lineStart = 0;
			if (lineEnd == -1) lineEnd = Content.Length;

			var currentLine = Content.Substring(lineStart, lineEnd - lineStart);

			return $"[{Line + 1:00}, {Column + 1:00}] {currentLine}";
		}
	}
}
