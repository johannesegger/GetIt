using System;
using OneOf;

namespace GetIt
{
    public abstract class SpeechBubble : OneOfBase<SpeechBubble.Say, SpeechBubble.Ask>
    {
        public class Say : SpeechBubble
        {
            public Say(string text)
            {
                Text = text ?? throw new ArgumentNullException(nameof(text));
            }
    
            public string Text { get; }
        }

        public class Ask : SpeechBubble
        {
            public Ask(string question, string answer, Action<string> answerHandler)
            {
                Question = question;
                Answer = answer ?? throw new ArgumentNullException(nameof(answer));
                AnswerHandler = answerHandler ?? throw new ArgumentNullException(nameof(answerHandler));
            }

            public string Question { get; }
            public string Answer { get; }
            public Action<string> AnswerHandler { get; }
        }
    }
}