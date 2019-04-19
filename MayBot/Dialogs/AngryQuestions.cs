using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace MayBot.Dialogs
{
    public class AngryQuestions : BaseDialogClass<QuestionsState>
    {
        public AngryQuestions(string dialogId, IStatePropertyAccessor<QuestionsState> userProfileStateAccessor, ILoggerFactory loggerFactory) : base(dialogId)
        {
            System.Diagnostics.Debug.WriteLine($"{Id} constructor");
            // UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    RespondToAPositiveQuestionStepAsync,
                    PromptForNextQuestionStepAsync,
                    ResponseToNextQuestionResultStepAsync,
            };

            AddDialog(new WaterfallDialog("QuestionsWaferfall", waterfallSteps));
            AddDialog(new TextPrompt("AskForText"));
        }

        // public IStatePropertyAccessor<QuestionsState> UserProfileAccessor { get; private set; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options is string options)
            {
                //var currentState = await UserProfileAccessor.GetAsync(stepContext.Context, () => new QuestionsState());
                //currentState.OriginalIntent = options;
                //await UserProfileAccessor.SetAsync(stepContext.Context, currentState, cancellationToken);

                this.State.OriginalIntent = options; 

            }

            return await stepContext.NextAsync();
        }
        
        private async Task<DialogTurnResult> RespondToAPositiveQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // var currentState = await UserProfileAccessor.GetAsync(stepContext.Context, () => new QuestionsState());
            var currentState = this.State;
            await stepContext.Context.SendActivityAsync($"I'm glad you asked me about {currentState.OriginalIntent} ");
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptForNextQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "Will you allow me to expand on that?",
                },
            };

            this.State.TimeAnswered = DateTime.Now;

            return await stepContext.PromptAsync("AskForText", opts);
        }

        private async Task<DialogTurnResult> ResponseToNextQuestionResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Last time changed " + this.State.TimeAnswered);
            if (stepContext.Result is string promptResult && !string.IsNullOrEmpty(promptResult))
            {
                if (promptResult == "yes")
                {
                    await stepContext.Context.SendActivityAsync("Good, it's important to clarify this");
                    return await stepContext.ReplaceDialogAsync("QuestionsWaferfall");
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("Let me just say, with our policies we will be strong and stable");
                }
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }

    }
}
