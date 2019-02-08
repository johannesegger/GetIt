module GetIt

type Say =
    { Text: string }

type Ask =
    { Question: string
      Answer: string
      AnswerHandler: string -> unit }
