using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;

namespace MayBot.Extensions
{
    public static class LUISExtensions
    {
        // pauliom.com
        public static double? GetSentimentScore(this RecognizerResult luisResult)
        {
            double? result = null;

            if (luisResult != null)
            {
                var data = luisResult.Properties["sentiment"];
                var sentimentValues = data as IDictionary<string, JToken>;
                var score = sentimentValues["score"] as JValue;
                result = (double)score.Value;
            }

            return result;
        }

        public static T GetEntity<T>(this RecognizerResult luisResult, string entityKey, string valuePropertyName = "text")
        {
            if (luisResult != null)
            {
                //// var value = (luisResult.Entities["$instance"][entityKey][0]["text"] as JValue).Value;
                var data = luisResult.Entities as IDictionary<string, JToken>;

                if (data.TryGetValue("$instance", out JToken value))
                {
                    var entities = value as IDictionary<string, JToken>;
                    if (entities.TryGetValue(entityKey, out JToken targetEntity))
                    {
                        var entityArray = targetEntity as JArray;
                        if (entityArray.Count > 0)
                        {
                            var values = entityArray[0] as IDictionary<string, JToken>;
                            if (values.TryGetValue(valuePropertyName, out JToken textValue))
                            {
                                if (textValue is JValue text)
                                {
                                    return (T)text.Value;
                                }
                            }
                        }
                    }
                }
            }

            return default(T);
        }
    }
}
