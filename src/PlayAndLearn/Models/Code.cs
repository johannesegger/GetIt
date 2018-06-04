using Microsoft.CodeAnalysis.Scripting;

namespace PlayAndLearn.Models
{
    public class Code
    {
        public Code(Script script, bool canExecute)
        {
            Script = script;
            CanExecute = canExecute;
        }

        public Script Script { get; }
        public bool CanExecute { get; }
    }
}