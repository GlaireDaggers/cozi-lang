using System;

namespace Compiler
{
    /// <summary>
    /// Represents a document which can be fed to a lexer and produces tokens
    /// </summary>
    public class Document
    {
        public readonly string Text;
        public readonly string SourcePath;

        public Document(string text, string path)
        {
            this.Text = text;
            this.SourcePath = path;
        }
    }
}