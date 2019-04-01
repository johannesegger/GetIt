namespace GetIt

type AskData =
    { Question: string
      Answer: string option }

type SpeechBubble =
    | Say of string
    | Ask of AskData
