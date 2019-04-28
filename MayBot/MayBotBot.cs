// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MayBot.Dialogs;
using MayBot.Extensions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace MayBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class MayBotBot : ActivityHandler
    {
        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public static readonly string LuisConfiguration = "MayBot";

        private readonly BotServices services;
        private readonly UserState userState;
        private readonly ConversationState conversationState;
        private readonly IStatePropertyAccessor<QuestionsState> niceQuestionsStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> dialogStateAccessor;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public MayBotBot(BotServices services, UserState userState, ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            this.services = services;

            // Verify LUIS configuration. Commented out as a dirty way of switching off LUIS connections for some demos
            ////if (!services.LuisServices.ContainsKey(LuisConfiguration))
            ////{
            ////    throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            ////}

            this.userState = userState ?? throw new ArgumentNullException(nameof(userState));
            this.conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            this.niceQuestionsStateAccessor = userState.CreateProperty<QuestionsState>(nameof(QuestionsState));
            this.dialogStateAccessor = conversationState.CreateProperty<DialogState>(nameof(DialogState));

            Dialogs = new DialogSet(dialogStateAccessor);
            Dialogs.Add(new NiceQuestions("NiceQuestions", niceQuestionsStateAccessor, loggerFactory));
            Dialogs.Add(new AngryQuestions("AngryQuestions", niceQuestionsStateAccessor, loggerFactory));

            // Example: Used to show how keeping a scratch dialog can help when learning about a new feature
            Dialogs.Add(new ScratchDialog("ScratchDialog", niceQuestionsStateAccessor, loggerFactory));

            // Example: Constructors. The following was used to show how constructor initializing occurs
            ////for (int index = 0; index < 10; index++)
            ////{
            ////    Dialogs.Add(new ChildDialog($"ChildDialog{index}"));
            ////}
        }

        private DialogSet Dialogs { get; set; }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Create a dialog context
            var dialogContext = await Dialogs.CreateContextAsync(turnContext);

            // Perform a call to LUIS to retrieve results for the current activity message.
            RecognizerResult luisResults = null;
            if (!string.IsNullOrEmpty(turnContext.Activity.Text))
            {
                luisResults = await this.services.LuisServices[LuisConfiguration].RecognizeAsync(turnContext, cancellationToken);
            }

            var topScoringIntent = luisResults?.GetTopScoringIntent();

            var topIntent = topScoringIntent.HasValue ? topScoringIntent.Value.intent : string.Empty;

            var preTranslatedText = turnContext.TurnState["PreTranslatedText"];
            var text = turnContext.Activity.Text;
            // Continue the current dialog
            var dialogResult = await dialogContext.ContinueDialogAsync();

            // if no one has responded,
            if (!dialogContext.Context.Responded)
            {
                // examine results from active dialog
                switch (dialogResult.Status)
                {
                    case DialogTurnStatus.Empty:

                        // Example: Constructors. The following was used to show how constructor initializing occurs
                        //////if (int.TryParse(text, out int dialogIndex) && dialogIndex < 10)
                        //////{
                        //////    this.Dialogs.Add(new ChildDialog($"ChildDialog{dialogIndex}"));
                        //////    await dialogContext.BeginDialogAsync($"ChildDialog{dialogIndex}");
                        //////}
                        //////else
                        //////{
                        //////    await dialogContext.BeginDialogAsync(nameof(AngryQuestions), text);
                        //////    //// await dialogContext.Context.SendActivityAsync("sorry please only enter a number < 10");
                        //////}

                        // Example: Used to show how keeping a scratch dialog can help when learning about a new feature
                        await dialogContext.BeginDialogAsync("ScratchDialog");
                        return;

                        var nextDialogId = nameof(NiceQuestions);
                        if (topScoringIntent.HasValue && topScoringIntent.Value.score < 0.5)
                        {
                            topIntent = "None";
                        }
                        else
                        {
                            var sentimentScore = luisResults.GetSentimentScore();
                            if (sentimentScore.HasValue && sentimentScore.Value < 0.5)
                            {
                                nextDialogId = nameof(AngryQuestions);
                            }
                        }

                        switch (topIntent)
                        {
                            case "Defence":
                            case "Power":
                            case "Health":
                                await dialogContext.BeginDialogAsync(nextDialogId, topIntent);
                                break;
                            case "Quit":
                                await dialogContext.CancelAllDialogsAsync();
                                break;
                            case "None":
                            default:
                                // Help or no intent identified, either way, let's provide some help.
                                // to the user
                                var returnMessage = MessageFactory.Text("I didn't understand what you just said to me.");
                                returnMessage.TextHighlights = new List<TextHighlight> { new TextHighlight("understand") };
                                await dialogContext.Context.SendActivityAsync(returnMessage);
                                break;
                        }

                        break;

                    case DialogTurnStatus.Waiting:
                        // The active dialog is waiting for a response from the user, so do nothing.
                        break;

                    case DialogTurnStatus.Complete:
                        await dialogContext.EndDialogAsync();
                        break;

                    default:
                        await dialogContext.CancelAllDialogsAsync();
                        break;
                }
            }
        }
    }
}
