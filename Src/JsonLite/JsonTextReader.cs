﻿using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Text;

namespace JsonLite
{
    public sealed class JsonTextReader : JsonReader
    {
        const int NothingInPeekBuffer = -2;
        const int EndOfStream = -1;

        readonly TextReader _textReader;
        int _peek = NothingInPeekBuffer;
        int _position;
        private int _currentTokenStartLineNumber = 0;
        private int _currentLineNumber = 0;
        private int _startPositionInLine = 0;
        private int _positionInLine = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textReader">The text reader to read the content from.</param>
        public JsonTextReader(TextReader textReader)
        {
            _textReader = textReader;
        }

        /// <summary>
        /// Peeks at the next character in the stream.
        /// </summary>
        /// <param name="ch">The character that is next in the stream.</param>
        /// <returns>true if a character was found, false if at the end of the stream.</returns>
        bool Peek(out char ch)
        {
            switch (_peek)
            {
                case NothingInPeekBuffer:
                    _peek = _textReader.Read();
                    return Peek(out ch);

                case EndOfStream:
                    ch = '\0';
                    return false;

                default:
                    ch = (char)_peek;
                    return true;
            }
        }

        /// <summary>
        /// Consumes the character from the stream.
        /// </summary>
        void Take()
        {
            _position++;
            _positionInLine++;
            _peek = NothingInPeekBuffer;
        }

        /// <summary>
        /// Returns the next token for consumption.
        /// </summary>
        /// <returns>The next token for consumption, or JsonToken.None when there are no more tokens remaining.</returns>
        public override JsonToken NextToken()
        {
            char ch;
            while (Peek(out ch))
            {
                _positionInLine++;
                switch (ch)
                {
                    case '{':
                        Take();
                        return JsonToken.StartObject;

                    case '}':
                        Take();
                        return JsonToken.EndObject;

                    case '[':
                        Take();
                        return JsonToken.StartArray;

                    case ']':
                        Take();
                        return JsonToken.EndArray;

                    case ':':
                        Take();
                        return JsonToken.Colon;

                    case ',':
                        Take();
                        return JsonToken.Comma;

                    case '"':
                        Take();
                        return StringToken();

                    case '-':
                    case '+':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return NumberToken();

                    case 'f':
                        return FalseToken();

                    case 't':
                        return TrueToken();

                    case 'n':
                        return NullToken();

                    default:
                        if (Char.IsWhiteSpace(ch))
                        {
                            if (ch == '\n')
                            {
                                _currentLineNumber++;
                                _positionInLine = 0;
                            }
                            Take();
                            continue;
                        }
                        throw new JsonUnexpectedCharacterException(ch, _position);
                }
            }

            return JsonToken.None;
        }

        /// <summary>
        /// Return a string token from the current position.
        /// </summary>
        /// <returns>The JSON token which represents the string that was made.</returns>
        JsonToken StringToken()
        {
            var text = new StringBuilder();
            var unicode = new char[4];

            char ch;
            while (Peek(out ch))
            {
                switch (ch)
                {
                    case '\\':
                        Take();
                        Peek(out ch);
                        switch (ch)
                        {
                            case '"':
                                Take();
                                text.Append('"');
                                break;

                            case '\\':
                                Take();
                                text.Append('\\');
                                break;

                            case '/':
                                Take();
                                text.Append('/');
                                break;

                            case 'b':
                                Take();
                                text.Append('\b');
                                break;

                            case 'f':
                                Take();
                                text.Append('\f');
                                break;

                            case 'n':
                                Take();
                                text.Append('\n');
                                _currentLineNumber++;
                                _positionInLine = 0;
                                _startPositionInLine = 0;
                                break;

                            case 'r':
                                Take();
                                text.Append('\r');
                                break;

                            case 't':
                                Take();
                                text.Append('\t');
                                break;

                            case 'u':
                                Take();
                                for (var i = 0; i < 4; i++)
                                {
                                    Peek(out ch);
                                    Take();

                                    unicode[i] = ch;
                                }
                                text.Append((char)UnicodeToInt(unicode));
                                break;

                            default:
                                throw new JsonException("Invalid escape character '{0}'.", ch);
                        }
                        break;

                    case '"':
                        Take();
                        var jsonToken = new JsonToken(JsonTokenKind.String, text.ToString()) { StartLinePosition = new LinePosition(_currentTokenStartLineNumber, _startPositionInLine), EndLinePosition = new LinePosition(_currentLineNumber, _positionInLine) };
                        _startPositionInLine = _currentTokenStartLineNumber;
                        _currentTokenStartLineNumber = _currentLineNumber;
                        return jsonToken;
                    default:
                        Take();
                        text.Append(ch);
                        break;
                }
            }

            throw new JsonUnexpectedEndException(_position);
        }

        /// <summary>
        /// Converts the given unicode character array into it's integer representation.
        /// </summary>
        /// <param name="unicode">The unicode character array to convert.</param>
        /// <returns>The integer value that represents the 4 byte unicode array.</returns>
        static int UnicodeToInt(char[] unicode)
        {
            return 
                HexCharToInt(unicode[0]) << 12 | 
                HexCharToInt(unicode[1]) << 8 | 
                HexCharToInt(unicode[2]) << 4 | 
                HexCharToInt(unicode[3]);
        }

        /// <summary>
        /// Convert a hex character to its integer value.
        /// </summary>
        /// <param name="ch">The hex character to convert.</param>
        /// <returns>The integer value that represents the hex character.</returns>
        static int HexCharToInt(char ch)
        {
            if (ch <= 57 && ch >= 48)
            {
                return ch - 48;
            }

            if (ch <= 70 && ch >= 65)
            {
                return ch - 55;
            }

            if (ch <= 102 && ch >= 97)
            {
                return ch - 87;
            }

            throw new JsonException("Invalid hex character '{0}'.", ch);
        }

        /// <summary>
        /// Return an number token.
        /// </summary>
        /// <returns>The token for the matching number.</returns>
        JsonToken NumberToken()
        {
            var value = new StringBuilder();

            char ch;
            while (Peek(out ch))
            {
                switch (ch)
                {
                    case '-':
                    case '+':
                        if (value.Length > 0)
                        {
                            // only allowed at the start
                            throw new JsonUnexpectedCharacterException(ch, _position);
                        }
                        Take();
                        value.Append(ch);
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        Take();
                        value.Append(ch);
                        break;

                    case 'e':
                    case 'E':
                        return ExponentToken(ch, value);

                    case '.':
                        Take();
                        return DecimalToken(value);

                    default:
                        if (IsTokenTerminator(ch))
                        {
                            return new JsonToken(JsonTokenKind.Integer, value.ToString());
                        }
                        throw new JsonUnexpectedCharacterException(ch, _position);
                }
            }

            return new JsonToken(JsonTokenKind.Integer, value.ToString());
        }

        /// <summary>
        /// Matches a fractional number token.
        /// </summary>
        /// <returns>The token that was matched.</returns>
        JsonToken DecimalToken(StringBuilder value)
        {
            value.Append('.');

            char ch;
            if (Peek(out ch) == false)
            {
                throw new JsonUnexpectedEndException(_position);
            }

            while (Peek(out ch))
            {
                switch (ch)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        Take();
                        value.Append(ch);
                        break;

                    case 'e':
                    case 'E':
                        return ExponentToken(ch, value);
                    
                    default:
                        if (IsTokenTerminator(ch))
                        {
                            return new JsonToken(JsonTokenKind.Fractional, value.ToString());
                        }
                        throw new JsonUnexpectedCharacterException(ch, _position);
                }
            }

            return new JsonToken(JsonTokenKind.Fractional, value.ToString());
        }

        /// <summary>
        /// Matches a fractional number token.
        /// </summary>
        /// <param name="ch">The exponent char that was found.</param>
        /// <param name="value">The current string buffer.</param>
        /// <returns>The token that was matched.</returns>
        JsonToken ExponentToken(char ch, StringBuilder value)
        {
            Take();
            value.Append(ch);

            if (Peek(out ch) == false)
            {
                throw new JsonUnexpectedEndException(_position);
            }

            // can optionally match the + or - signs 
            if (ch == '+' || ch == '-')
            {
                Take();
                value.Append(ch);

                if (Peek(out ch) == false)
                {
                    throw new JsonUnexpectedEndException(_position);
                }
            }

            while (Peek(out ch))
            {
                switch (ch)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        Take();
                        value.Append(ch);
                        break;

                    default:
                        if (IsTokenTerminator(ch))
                        {
                            return new JsonToken(JsonTokenKind.Fractional, value.ToString());
                        }
                        throw new JsonUnexpectedCharacterException(ch, _position);
                }
            }

            return new JsonToken(JsonTokenKind.Fractional, value.ToString());
        }

        /// <summary>
        /// Matches the False token.
        /// </summary>
        /// <returns>The token that was matched.</returns>
        JsonToken FalseToken()
        {
            var position = _position;

            if (IsMatch("false") == false)
            {
                throw new JsonUnexpectedCharacterException('f', position + 1);
            }

            return JsonToken.False;
        }

        /// <summary>
        /// Matches the True token.
        /// </summary>
        /// <returns>The token that was matched.</returns>
        JsonToken TrueToken()
        {
            var position = _position;

            if (IsMatch("true") == false)
            {
                throw new JsonUnexpectedCharacterException('t', position + 1);
            }

            return JsonToken.True;
        }

        /// <summary>
        /// Matches the Null token.
        /// </summary>
        /// <returns>The token that was matched.</returns>
        JsonToken NullToken()
        {
            var position = _position;

            if (IsMatch("null") == false)
            {
                throw new JsonUnexpectedCharacterException('n', position + 1);
            }

            return JsonToken.Null;
        }

        /// <summary>
        /// Returns a value indicating whether the current text reader can match a constant token.
        /// </summary>
        /// <param name="match">The characters to match.</param>
        /// <returns>true if the constant token could be matched, false if not.</returns>
        bool IsMatch(string match)
        {
            for (var i = 0; i < match.Length; i++)
            {
                char ch;
                if (Peek(out ch) == false || ch != match[i])
                {
                    return false;
                }

                Take();
            }

            return true;
        }

        /// <summary>
        /// Returns a value indicating whether or not the given character can be considered a valid ending for a token.
        /// </summary>
        /// <param name="ch">The character to test.</param>
        /// <returns>true if the given character is a valid ending to a token, false if not.</returns>
        static bool IsTokenTerminator(char ch)
        {
            return Char.IsWhiteSpace(ch) || ch == ',' || ch == '}' || ch == ']';
        }
    }
}