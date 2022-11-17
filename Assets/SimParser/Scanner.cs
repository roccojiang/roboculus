using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SimParser {
/// <summary>
/// Delegate type for any such parser function.
/// </summary>
/// <typeparam name="T">Type of thing to parse.</typeparam>
public delegate Either<string, T> Parser<T>(string input);

/// <summary>
/// Delegate type for any TryParse-like functions.
/// </summary>
/// <typeparam name="T">Type of the thing to parse.</typeparam>
public delegate bool SafeReader<T>(string input, out T result);

/// <summary>
/// Token scanner class for C#.
/// </summary>
public class Scanner : StreamReader {
  public Scanner(Stream stream) : base(stream) {}

  public Scanner(string path) : base(path) {}

  private readonly Stack<char> _pushbackBuffer = new();

  /// <summary>
  /// Read the next whitespace-separated token from the stream.
  /// NOTE: Not thread safe!
  /// </summary>
  private string NextToken(bool ignoreLF = false) {
    StringBuilder internalBuilder = new();

    do {
      // Cannot read anymore.
      if (EndOfStream)
        break;
      // XXX: This is a very hacky fix! Please replace with a proper check!
      if (BaseStream is NetworkStream { DataAvailable : false })
        break;

      int next = _pushbackBuffer.Count > 0 ? _pushbackBuffer.Pop() : Read();

      // No more chars.
      if (next < 0)
        break;

      // No extended ASCII or unicode allowed:
      if (next >= 127)
        continue;

      char nextChar = (char)next;

      // If end of token, break. Otherwise add to builder.
      if (char.IsWhiteSpace(nextChar)) {
        ConsumeWhitespace(ignoreLF, nextChar);
        break;
      }

      internalBuilder.Append(nextChar);
    } while (true);

    return internalBuilder.ToString();
  }

  private IEnumerable<string> NextTokensUntilLF() {
    List<string> tokens = new();

    string token;
    do {
      token = NextToken(true);
      if (!string.IsNullOrWhiteSpace(token))
        tokens.Add(token);
    } while (!string.IsNullOrWhiteSpace(token) ||
             _pushbackBuffer.TryPeek(out char first) && first != '\n');
    _pushbackBuffer.Clear();

    return tokens;
  }

  // Consume remaining whitespace until the next non-whitespace character
  // in the stream.
  private void ConsumeWhitespace(bool ignoreLF = false, char ch = '\0') {
    int next = ignoreLF ? (int)ch : Read();

    // While there are still more characters left in the stream,
    // consume all whitespace until the next non-whitespace
    // character.
    // Then push that character into the pushback buffer.
    while (next > 0) {
      char nextChar = (char)next;
      if ((ignoreLF && nextChar == '\n') || !char.IsWhiteSpace(nextChar)) {
        _pushbackBuffer.Push(nextChar);
        return;
      }

      next = Read();
    }
  }

  // Convert a TryParse-like function into an IEither-returning parser.
  public static Parser<T> ConvertToParser<T>(SafeReader<T> reader) => token => {
    bool success = reader(token, out T result);

    return success ? Either<string, T>.ToRight(result)
                   : Either<string, T>.ToLeft("Unexpected Token: " + token);
  };

  // Parses a parsable token into something, or returns the token if it fails.
  public Either<string, T>
  ParseNext<T>(Parser<T> parser,
               bool ignoreLF = false) => parser(NextToken(ignoreLF));

  // Parses an enumerable of tokens.
  public Either<string, IEnumerable<T>> ParseManyToLF<T>(Parser<T> parser) =>
      NextTokensUntilLF().Traverse(s => parser(s));

  // Overload for ParseNext used for testing.
  [Obsolete(
      "Use the other ParseNext overload with a cached Parser instead for better performance.")]
  public Either<string, T> ParseNext<T>(SafeReader<T> reader,
                                        bool ignoreLF = false) =>
      ParseNext(ConvertToParser(reader), ignoreLF);
}
}
