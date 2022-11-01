using System.Collections.Generic;
using System.IO;
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

  private Stack<char> _pushbackBuffer = new();

  /// <summary>
  /// Read the next whitespace-separated token from the stream.
  /// NOTE: Not thread safe!
  /// </summary>
  public string NextToken() {
    StringBuilder internalBuilder = new StringBuilder();

    do {
      int next = _pushbackBuffer.Count > 0 ? _pushbackBuffer.Pop() : Read();

      // No more chars.
      if (next < 0)
        break;

      char nextChar = (char)next;

      // If end of token, break. Otherwise add to builder.
      if (char.IsWhiteSpace(nextChar)) {
        next = Read();

        // While there are still more characters left in the stream,
        // consume all whitespace until the next non-whitespace
        // character.
        // Then push that character into the pushback buffer.
        while (next > 0) {
          nextChar = (char)next;
          if (!char.IsWhiteSpace(nextChar)) {
            _pushbackBuffer.Push(nextChar);
            break;
          }

          next = Read();
        }

        break;
      }

      internalBuilder.Append(nextChar);
    } while (true);

    return internalBuilder.ToString();
  }

  // Convert a TryParse-like function into an IEither-returning parser.
  public static Parser<T> ConvertToParser<T>(SafeReader<T> reader) => token => {
    bool success = reader(token, out T result);

    return success ? Either<string, T>.ToRight(result)
                   : Either<string, T>.ToLeft("Unexpected Token: " + token);
  };

  // Parses a parsable token into something, or returns the token if it fails.
  public Either<string, T>
  ParseNext<T>(Parser<T> parser) => parser(NextToken());

  public Either<string, T>
  ParseNext<T>(SafeReader<T> reader) => ParseNext(ConvertToParser(reader));
}
}
