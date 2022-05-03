using Microsoft.CodeAnalysis.Text;

namespace JsonLite.Ast
{
    public abstract class JsonValue
    {
        public LinePosition StartLinePosition { get; set; }
        public LinePosition EndLinePosition { get; set; }

        /// <summary>
        /// Output the JSON value as a string.
        /// </summary>
        /// <param name="prettify">Indicates whether the output should be prettified.</param>
        /// <returns>The JSON value of the node.</returns>
        public string Stringify(bool prettify = false)
        {
            if (prettify)
            {
                return Stringify(new JsonPrettyStringVisitor());
            }

            return Stringify(new JsonStringifyVisitor());
        }

        /// <summary>
        /// Output the JSON value as a string.
        /// </summary>
        /// <param name="visitor">The stringify visitor.</param>
        /// <returns>The JSON value of the node.</returns>
        public string Stringify(IJsonStringifyVisitor visitor)
        {
            return visitor.Stringify(this);
        }
    }
}