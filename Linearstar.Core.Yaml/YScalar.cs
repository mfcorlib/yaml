using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Linearstar.Core.Yaml
{
	public class YScalar : YNode
	{
		public object Value { get; set; }

		public YScalar() =>
			Style = YNodeStyle.Flow;

		public YScalar(object value)
			: this() =>
			Value = value;

		internal static new YNode Parse(Tokenizer tokenizer)
		{
			switch (tokenizer.Current.Kind)
			{
				case TokenKind.StringDouble:
				case TokenKind.StringSingle:
				case TokenKind.StringFolding:
				case TokenKind.StringLiteral:
					{
						var kind = tokenizer.Current.Kind;
						var value = tokenizer.Current.Value;

						if (tokenizer.Current.Kind == TokenKind.StringDouble)
							value = UnescapeString(value);

						tokenizer.MoveNext();

						return new YScalar(value) { Style = kind == TokenKind.StringFolding || kind == TokenKind.StringLiteral ? YNodeStyle.Block : YNodeStyle.Flow };
					}
				case TokenKind.StringPlain:
					{
						var value = tokenizer.Current.Value;

						tokenizer.MoveNext();

						if (string.IsNullOrEmpty(value))
							return new YScalar(null);

						var op = value[0] == '-' ? -1 : 1;
						var numValue = value[0] == '+' || value[0] == '-' ? value.Substring(1) : value;

						if (numValue.StartsWith("0x") && numValue.Substring(2).Cast<char>().All(i => char.IsDigit(i) || "ABCDEFabcdef".IndexOf(i) != -1))
							return new YScalar(int.Parse(numValue.Substring(2), NumberStyles.HexNumber) * op);
						else if (numValue.StartsWith("0") && numValue.Substring(1).Cast<char>().All(i => "01234567".IndexOf(i) != -1))
							return new YScalar(Convert.ToInt32(numValue.Substring(1), 8) * op);
						else if (numValue.StartsWith("0b") && numValue.Substring(2).Cast<char>().All(i => i == '0' || i == '1'))
							return new YScalar(Convert.ToInt32(numValue.Substring(2), 2) * op);
						else if (numValue.Cast<char>().All(char.IsDigit))
							return new YScalar(int.Parse(numValue) * op);
						else if (numValue.Cast<char>().All(i => char.IsDigit(i) || i == '.'))
							return new YScalar(double.Parse(numValue) * op);
						else if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
							return new YScalar(null);
						else if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
							return new YScalar(bool.Parse(value));
						else
							return new YScalar(value);
					}
				default:
					return null;
			}
		}

		public static string EscapeString(string str) =>
			str.Replace("\\", "\\\\")
			   .Replace("\x22", "\\\"")
			   .Replace("\x07", "\\a")
			   .Replace("\x08", "\\b")
			   .Replace("\x1b", "\\e")
			   .Replace("\x0c", "\\f")
			   .Replace("\x0a", "\\n")
			   .Replace("\x0d", "\\r")
			   .Replace("\0x9", "\\t")
			   .Replace("\x0b", "\\v")
			   .Replace("\x00", "\\0")
			   .Replace("\xA0", "\\_")
			   .Replace("\x85", "\\N")
			   .Replace("\u2028", "\\L")
			   .Replace("\u2029", "\\P");

		static readonly Regex unicodeEscape = new Regex(@"\\(?:x(?<num>[0-9A-Fa-f]{2})|u(?<num>[0-9A-Fa-f]{4})|U(?<num>[0-9A-Fa-f]{8}))");

		public static string UnescapeString(string str)
		{
			str = str.Replace("\\\"", "\x22")
					 .Replace("\\a", "\x07")
					 .Replace("\\b", "\x08")
					 .Replace("\\e", "\x1b")
					 .Replace("\\f", "\x0c")
					 .Replace("\\n", "\x0a")
					 .Replace("\\r", "\x0d")
					 .Replace("\\t", "\0x9")
					 .Replace("\\v", "\x0b")
					 .Replace("\\0", "\x00")
					 .Replace("\\ ", "\x20")
					 .Replace("\\_", "\xA0")
					 .Replace("\\N", "\x85")
					 .Replace("\\L", "\u2028")
					 .Replace("\\P", "\u2029");

			str = unicodeEscape.Replace(str, m => char.ConvertFromUtf32(int.Parse(m.Groups["num"].Value, NumberStyles.HexNumber)));

			return str.Replace("\\\\", "\\");
		}

		public override string ToString(YNodeStyle style)
		{
			switch (Value)
			{
				case bool boolValue:
					return $"!!bool \"{boolValue.ToString().ToLower()}\"";
				case int intValue:
					return $"!!int \"{intValue}\"";
				case double floatValue:
					return $"!!float \"{floatValue}\"";
				case string strValue:
					return style == YNodeStyle.Block
						? $"!!str |-\n{AddIndent(strValue)}"
						: $"!!str \"{EscapeString(strValue)}\"";
				default:
					return "!!null \"null\"";
			}
		}

		public override string ToYamlString(YNodeStyle style)
		{
			switch (Value)
			{
				case bool boolValue:
					return boolValue.ToString().ToLower();
				case int intValue:
					return intValue.ToString();
				case double floatValue:
					return floatValue.ToString();
				case string strValue:
					return style == YNodeStyle.Block
						? $"|-\n{AddIndent(strValue)}"
						: strValue.IndexOfAny(new[] { '-', '{', '}', '[', ']', '|', '>', '?' }) == 0 || strValue.IndexOfAny(new[] { '\a', '\b', '\t', '\0', '\r', ':' }) != -1
							? $"\"{EscapeString(strValue)}\""
							: strValue;
				default:
					return "null";
			}
		}

		public override bool Equals(object obj) =>
			obj is YScalar scalar &&
			(scalar.Value?.Equals(Value) ?? Value == null);

		public override int GetHashCode() =>
			typeof(YScalar).GetHashCode() ^ (Value?.GetHashCode() ?? 0);

		public static explicit operator bool(YScalar scalar) => (bool)scalar.Value;
		public static explicit operator int(YScalar scalar) => (int)scalar.Value;
		public static explicit operator double(YScalar scalar) => (double)scalar.Value;
		public static explicit operator string(YScalar scalar) => (string)scalar.Value;

		public static implicit operator YScalar(bool value) => new YScalar(value);
		public static implicit operator YScalar(int value) => new YScalar(value);
		public static implicit operator YScalar(double value) => new YScalar(value);
		public static implicit operator YScalar(string value) => new YScalar(value);
	}
}
