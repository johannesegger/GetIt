namespace GetIt

type SayData =
    { Text: string }

type AskData =
    { Question: string
      Answer: string }

type SpeechBubble =
    | Say of SayData
    | Ask of AskData
