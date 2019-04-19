using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MayBot.Dialogs
{
    public class ScratchDialog : ComponentDialog
    {
        public ScratchDialog(string dialogId, IStatePropertyAccessor<QuestionsState> userProfileStateAccessor, ILoggerFactory loggerFactory) : base(dialogId)
        {
            System.Diagnostics.Debug.WriteLine($"{Id} constructor");
            UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    PromptForNextQuestionStepAsync,
                    ResponseToNextQuestionResultStepAsync,
            };

            AddDialog(new WaterfallDialog("NiceQuestionsWaferfall", waterfallSteps));
            AddDialog(new TextPrompt("AskForText"));
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
        
        private async Task<DialogTurnResult> PromptForNextQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var second = DateTime.Now.Second;
            List<CardAction> cardActions = new List<CardAction>()
            {
                new CardAction(ActionTypes.ImBack, title:$"A lovely red {second}", value: $"Red {second}"),
                new CardAction(ActionTypes.PostBack, title:"A rich blue", value: "Blue")
            };

            List<Attachment> attachments = new List<Attachment>();

            HeroCard heroCard = new HeroCard(title: "Colours", buttons: cardActions);
            attachments.Add(heroCard.ToAttachment());

            List<CardAction> suggestedCardActions = new List<CardAction>()
            {
                new CardAction(ActionTypes.PostBack, title:$"Travel North {second}", value: $"North {second}"),
                new CardAction(ActionTypes.PostBack, title:"Travel South", value: "South")
            };
            
            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "Please enter a direction or color",
                    SuggestedActions = new SuggestedActions(actions: suggestedCardActions),
                    Attachments = attachments
                },
            };
            return await stepContext.PromptAsync("AskForText", opts);
        }

        private async Task<DialogTurnResult> ResponseToNextQuestionResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is string promptResult && !string.IsNullOrEmpty(promptResult))
            {
                if (promptResult != "quit")
                {
                    await stepContext.Context.SendActivityAsync($"you said {stepContext.Result}, looping... ");
                    return await stepContext.ReplaceDialogAsync("NiceQuestionsWaferfall");
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("finished");
                }
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }

    }
}
