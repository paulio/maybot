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
    public class NiceQuestions : ComponentDialog
    {
        public NiceQuestions(string dialogId, IStatePropertyAccessor<QuestionsState> userProfileStateAccessor, ILoggerFactory loggerFactory) : base(dialogId)
        {
            System.Diagnostics.Debug.WriteLine($"{Id} constructor");
            UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    RespondToAPositiveQuestionStepAsync,
                    PromptForNextQuestionStepAsync,
                    ResponseToNextQuestionResultStepAsync,
            };

            AddDialog(new WaterfallDialog("NiceQuestionsWaferfall", waterfallSteps));
            AddDialog(new TextPrompt("AskForText"));
            AddDialog(new TextPrompt("YesNoPrompt", YesNoValidator));
        }

        private Task<bool> YesNoValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var userResponse = promptContext.Recognized.Value;
            var isValid = (userResponse.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
                userResponse.Equals("no", StringComparison.InvariantCultureIgnoreCase));

            List<CardAction> suggestedCardActions = new List<CardAction>()
            {
                new CardAction(ActionTypes.PostBack, title:"Yes", value: "yes"),
                new CardAction(ActionTypes.PostBack, title:"No", value: "no")
            };

            var randomResponses = new string[] { "Sorry I didn't understand that response. ", "Can you try again? ", "What? " };

            var aResponse = randomResponses[new Random().Next(randomResponses.Length)];
            promptContext.Options.RetryPrompt = new Activity
            {
                Type = ActivityTypes.Message,
                Text = aResponse + "Shall I expand on that? (yes or no)",
                SuggestedActions = new SuggestedActions(actions: suggestedCardActions)
            };


            return Task.FromResult(isValid);
        }


        private async Task<DialogTurnResult> PromptForNextQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            List<CardAction> suggestedCardActions = new List<CardAction>()
            {
                new CardAction(ActionTypes.PostBack, title:"Yes", value: "yes"),
                new CardAction(ActionTypes.PostBack, title:"No", value: "no")
            };

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "Shall I expand on that?",
                    SuggestedActions = new SuggestedActions(actions: suggestedCardActions)
                },
                RetryPrompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "Sorry I didn't understand that response. Shall I expand on that? (yes or no)",
                    SuggestedActions = new SuggestedActions(actions: suggestedCardActions)
                },
            };
            return await stepContext.PromptAsync("YesNoPrompt", opts);
        }

        public IStatePropertyAccessor<QuestionsState> UserProfileAccessor { get; private set; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options is string options)
            {
                var currentState = await UserProfileAccessor.GetAsync(stepContext.Context, () => new QuestionsState());
                currentState.OriginalIntent = options;
                await UserProfileAccessor.SetAsync(stepContext.Context, currentState, cancellationToken);
            }

            return await stepContext.NextAsync();
        }
        
        private async Task<DialogTurnResult> RespondToAPositiveQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var currentState = await UserProfileAccessor.GetAsync(stepContext.Context, () => new QuestionsState());
            await stepContext.Context.SendActivityAsync($"Thanks for asking about {currentState.OriginalIntent} ");
            return await stepContext.NextAsync();
        }

       

        private async Task<DialogTurnResult> ResponseToNextQuestionResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is string promptResult && !string.IsNullOrEmpty(promptResult))
            {
                if (promptResult.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
                {
                    await stepContext.Context.SendActivityAsync("Thanks, I will");
                    return await stepContext.ReplaceDialogAsync("NiceQuestionsWaferfall");
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("oh well...");
                }
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }

    }
}
