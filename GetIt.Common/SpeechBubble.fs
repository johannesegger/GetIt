namespace GetIt

type AskData =
    { Question: string
      Answer: string }

type SpeechBubble =
    | Say of string
    | Ask of AskData
