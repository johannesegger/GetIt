using System;

namespace PlayAndLearn.Models
{
    public class SpeechBubble
    {
        public static readonly SpeechBubble Empty = new SpeechBubble("", TimeSpan.Zero);
        
        public SpeechBubble(string text, TimeSpan duration)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Duration = duration;
        }

        public string Text { get; }

        public TimeSpan Duration { get; }
    }
}