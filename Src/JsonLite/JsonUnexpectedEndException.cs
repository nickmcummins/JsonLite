﻿namespace JsonLite
{
    public sealed class JsonUnexpectedEndException : JsonException
    {
        readonly int _position;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="position">The position at which the last character occurred.</param>
        public JsonUnexpectedEndException(int position) : base("Unexpected end at at position {0}.", position)
        {
            _position = position;
        }

        /// <summary>
        /// Gets the position at which the last character occurred.
        /// </summary>
        public int Position
        {
            get {  return _position; }
        }
    }
}
