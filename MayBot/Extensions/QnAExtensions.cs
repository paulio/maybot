using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.QnA;

namespace MayBot.Extensions
{
    public static class QnAExtensions
    {
        private static Dictionary<string, List<string>> RandomAnswers = new Dictionary<string, List<string>>
            {
                { "I think it's best if we stick to a professional relationship.",
                    new List<string>
                    {
                        "I, erm, thank you but I'm already married",
                        "No thank you, I'm happily married"
                    }
                }
            };

        private static Random RandomNumber = new Random(Environment.TickCount);

        public static string RandomAnswer(this QueryResult queryResult)
        {
            if (RandomAnswers.ContainsKey(queryResult.Answer))
            {
                var randomAnswerSet = RandomAnswers[queryResult.Answer];
                int next = RandomNumber.Next(randomAnswerSet.Count + 1);
                if (next < randomAnswerSet.Count)
                {
                    return randomAnswerSet[next];
                }
            }

            return queryResult.Answer;
        }
    }
}
