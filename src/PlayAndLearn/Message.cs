using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using OneOf;
using PlayAndLearn.Models;

namespace PlayAndLearn
{
    public class Message : OneOfBase<
        Message.ChangeSceneSize,
        Message.ChangeCode,
        Message.EnableCodeExecution,
        Message.CompilationError,
        Message.CompilationException,
        Message.ExecuteCode>
    {
        public class ChangeSceneSize : Message
        {
            public ChangeSceneSize(Size newSize)
            {
                NewSize = newSize;
            }

            public Size NewSize { get; }
        }

        public class ChangeCode : Message
        {
            public ChangeCode(string code)
            {
                Code = code;
            }

            public string Code { get; }
        }

        public class EnableCodeExecution : Message
        {
        }

        public class CompilationError : Message
        {
            public CompilationError(IEnumerable<Diagnostic> errors)
            {
                Errors = errors.ToList();
            }

            public IReadOnlyCollection<Diagnostic> Errors { get; }
        }

        public class CompilationException : Message
        {
            public CompilationException(Exception exception)
            {
                Exception = exception;
            }

            public Exception Exception { get; }
        }

        public class ExecuteCode : Message
        {
        }
    }
}