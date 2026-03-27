namespace Magic.Kernel.Compilation.Ast
{
    /// <summary>Raw high-level statement text from non-asm block.</summary>
    public class StatementLineNode : AstNode
    {
        /// <summary>1-based строка AGI для statement-блока.</summary>
        public int SourceLine { get; set; }

        public string Text { get; set; } = string.Empty;
    }
}
