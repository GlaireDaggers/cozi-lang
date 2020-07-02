using System;
using System.Text.RegularExpressions;

namespace Compiler.Utils
{
    public static class StringExtensions
    {
        public static StringSpan Slice(this string self, int index)
        {
            return new StringSpan(self, index, self.Length - index);
        }

        public static StringSpan Slice(this string self, int index, int length)
        {
            return new StringSpan(self, index, length);
        }
    }

    public static class RegexExtensions
    {
        public static Match Match(this Regex self, StringSpan span)
        {
            return self.Match(span.SourceString, span.StartIndex, span.Length);
        }

        public static Match Match(this Regex self, StringSpan span, int index)
        {
            return Match(self, span.Slice(index));
        }

        public static Match Match(this Regex self, StringSpan span, int index, int length)
        {
            return Match(self, span.Slice(index, length));
        }
    }

    /// <summary>
    /// Represents a slice of another string via a reference, a start offset, and a length
    /// Can also be compared against strings and other StringSpans
    /// </summary>
    public struct StringSpan : IEquatable<StringSpan>, IEquatable<String>
    {
        public readonly string SourceString;
        public readonly int StartIndex;
        public readonly int Length;

        public char this[int index]
        {
            get
            {
                return SourceString[index + StartIndex];
            }
        }

        public StringSpan(string source, int start, int length)
        {
            if( start < 0 || start >= source.Length )
                throw new ArgumentOutOfRangeException("start");

            if( length > source.Length - start )
                throw new ArgumentOutOfRangeException("length");

            this.SourceString = source;
            this.StartIndex = start;
            this.Length = length;
        }

        public StringSpan Slice(int index)
        {
            return SourceString.Slice(this.StartIndex + index);
        }

        public StringSpan Slice(int index, int length)
        {
            return SourceString.Slice(this.StartIndex + index, length);
        }

        public bool StartsWith(string str)
        {
            if(str.Length > Length)
                return false;

            return Slice(0, str.Length) == str;
        }

        public override string ToString()
        {
            return SourceString.Substring(StartIndex, Length);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = (hash * 37) + SourceString.GetHashCode();
            hash = (hash * 37) + StartIndex;
            hash = (hash * 37) + Length;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if(obj is string)
            {
                return Equals((string)obj);
            }
            else if(obj is StringSpan)
            {
                return Equals((StringSpan)obj);
            }

            return false;
        }

        public bool Equals(StringSpan other)
        {
            if( Length != other.Length )
                return false;

            // fast path: being sliced from the same string at the same position implies equality
            if( SourceString == other.SourceString && StartIndex == other.StartIndex )
                return true;

            // otherwise compare character-by-character
            for(int i = 0; i < Length; i++)
            {
                if( this[i] != other[i] )
                    return false;
            }

            return true;
        }

        public bool Equals(String other)
        {
            if( Length != other.Length )
                return false;

            for(int i = 0; i < Length; i++)
            {
                if( this[i] != other[i] )
                    return false;
            }

            return true;
        }

        public static bool operator==(StringSpan lhs, string rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(StringSpan lhs, string rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}