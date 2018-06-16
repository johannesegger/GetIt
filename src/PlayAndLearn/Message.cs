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
        Message.CompileCode,
        Message.StartCodeExecution,
        Message.ContinueCodeExecution,
        Message.ResetPlayerPosition,
        Message.StartDragPlayer,
        Message.DragPlayer,
        Message.StopDragPlayer>
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

        public class CompileCode : Message
        {
        }

        public class StartCodeExecution : Message
        {
        }

        public class ContinueCodeExecution : Message
        {
        }

        public class ResetPlayerPosition : Message
        {
        }

        public class StartDragPlayer : Message
        {
            public StartDragPlayer(Position position)
            {
                Position = position;
            }

            public Position Position { get; }
        }

        public class DragPlayer : Message
        {
            public DragPlayer(Position position)
            {
                Position = position;
            }

            public Position Position { get; }
        }

        public class StopDragPlayer : Message
        {
        }
    }
}