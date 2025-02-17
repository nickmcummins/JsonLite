﻿using System.Diagnostics;

namespace JsonLite.Ast
{
    [DebuggerDisplay("[String] {Value}")]
    public sealed class JsonString : JsonValue, IJsonPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The text string.</param>
        public JsonString(string value)
        {
            Value = value;
        }

        public JsonString(JsonToken jsonToken)
        {
            Value = jsonToken.Text;
            StartLinePosition = jsonToken.StartLinePosition;
            EndLinePosition = jsonToken.EndLinePosition;
        }

        /// <summary>
        /// Gets the underlying CLR value.
        /// </summary>
        /// <returns>The CLR value.</returns>
        object IJsonPrimitive.GetClrValue()
        {
            return Value;
        }

        /// <summary>
        /// Gets the text string.
        /// </summary>
        public string Value { get; }

        public override string ToString()
        {
            return $"jsonString(Value={Value})";
        }
    }
}