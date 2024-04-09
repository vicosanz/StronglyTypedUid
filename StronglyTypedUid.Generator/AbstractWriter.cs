using System;
using System.Collections.Generic;
using System.Text;

namespace StronglyTypedUid.Generator
{
    public abstract class AbstractWriter
    {
        internal StringBuilder builder = new();
        private string _lineStartIndentation = "";
        private const string _indent = "    ";
        private bool _isLineStart = true;

        public string GeneratedText() => builder.ToString();

        protected void Write(string text)
        {
            if (_isLineStart)
            {
                builder.Append(_lineStartIndentation);
                _isLineStart = false;
            }

            builder.Append(text);
        }

        protected void WriteLine(string? text = null)
        {
            if (text != null) Write(text);
            builder.AppendLine();
            _isLineStart = true;
        }

        protected void WriteNested(Action action)
        {
            var oldLineStartIndentation = _lineStartIndentation;
            _lineStartIndentation += _indent;
            action();
            _lineStartIndentation = oldLineStartIndentation;
        }

        protected void WriteNested(string open, string close, Action action)
        {
            if (!_isLineStart)
                WriteLine();
            WriteLine(open);
            WriteNested(action);
            WriteLine(close);
        }

        protected void WriteBrace(Action action)
        {
            WriteNested("{", "}", action);
        }

        protected void WriteBrace(string? text, Action action)
        {
            WriteLine(text);
            WriteNested("{", "}", action);
        }
    }
}
