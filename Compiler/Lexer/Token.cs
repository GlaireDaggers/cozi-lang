using System;

namespace Cozi.Compiler
{
    using Cozi.Compiler.Utils;

    /// <summary>
    /// Represents a token's classification
    /// </summary>
    public enum TokenType
    {
        None,

        Integer,
        HexInteger,
        OctInteger,
        BinInteger,
        Float,
        Double,
        String,
        Identifier,
        Boolean,
        
        Var,
        Const,
        Function,
        Return,
        Struct,
        Module,
        Interface,
        Implement,
        Unsafe,
        Ref,
        Clone,
        Init,
        New,
        This,
        Destructor,
        Import,
        Export,
        Extern,
        Cast,

        If,
        Else,
        For,
        In,
        While,
        Do,
        Continue,
        Break,

        Semicolon,
        Colon,
        OpenCurlyBrace,
        CloseCurlyBrace,
        OpenParenthesis,
        CloseParenthesis,
        OpenAngleBracket,
        CloseAngleBracket,
        OpenBracket,
        CloseBracket,
        Comma,

        Range,

        Equals,
        PlusEquals,
        MinusEquals,
        TimesEquals,
        DivideEquals,
        ModuloEquals,
        ShiftLeftEquals,
        ShiftRightEquals,
        AndEquals,
        XorEquals,
        OrEquals,

        Asterisk,
        Ampersand,
        Plus,
        Minus,
        ForwardSlash,
        Modulo,
        ShiftLeft,
        ShiftRight,
        LessThanEqualTo,
        GreaterThanEqualTo,
        EqualTo,
        NotEqualTo,

        BitwiseOr,
        BitwiseXor,
        BitwiseNot,
        
        LogicalAnd,
        LogicalOr,
        LogicalNot,

        Increment,
        Decrement,

        Dot,
    }

    /// <summary>
    /// Represents a single logical "word" of the source text input
    /// </summary>
    public struct Token
    {
        public Document SourceDocument;
        public TokenType Type;
        public StringSpan Value;

        public override string ToString()
        {
            return $"{Value} <{Type}>";
        }
    }
}