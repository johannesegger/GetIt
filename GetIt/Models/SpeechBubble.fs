namespace GetIt

type SayData =
    { Text: string }

type AskData =
    { Question: string
      Answer: string
      AnswerHandler: string -> unit }

type SpeechBubble =
    | Say of SayData
    | Ask of AskData
