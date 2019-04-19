using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace MayBot.Dialogs
{
    public class ChildDialog : ComponentDialog
    {
        public ChildDialog(string dialogId) : base(dialogId)
        {
            System.Diagnostics.Debug.WriteLine($"{Id} constructor");
            var waterfallSteps = new WaterfallStep[]
           {
                    StepAAsync,
                    StepBAsync,
           };

            AddDialog(new WaterfallDialog("QuestionsWaferfall", waterfallSteps));
            AddDialog(new TextPrompt("AskForText"));

            if (dialogId == "ChildDialog3")
            {
                for(int index = 0; index < 10; index++)
                {
                    AddDialog(new ChildDialog($"SubDialog{index}"));
                }
            }
        }

        private async Task<DialogTurnResult> StepAAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync($"Hi from {Id}");
            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "I'll echo what you type",
                },
            };

            return await stepContext.PromptAsync("AskForText", opts);
        }

        private async Task<DialogTurnResult> StepBAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync($"Echo from {Id}: {stepContext.Result}");
            return new DialogTurnResult(DialogTurnStatus.Complete);
        }

       
    }
}
