// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiTurnPromptBot;

namespace Microsoft.BotBuilderSamples
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler where T : Dialog 
    {
        protected readonly int TimeoutSeconds;
        protected readonly IStatePropertyAccessor<DialogState> DialogState;
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly ILogger Logger;
        private readonly ConcurrentDictionary<string, TimeoutConversationReference> _conversationReferences;

        public DialogBot(IConfiguration configuration, ConcurrentDictionary<string, TimeoutConversationReference> conversationReferences, ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        {
            _conversationReferences = conversationReferences;
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
            DialogState = ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            TimeoutSeconds = configuration.GetValue<int>("ConversationTimeoutSeconds");
        }

        private async Task AddOrUpdateConversationReference(ITurnContext turnContext)
        {
            var activity = turnContext.Activity;
            TimeoutConversationReference timeoutConversation = null;
            if (_conversationReferences.TryGetValue(activity.Conversation.Id, out timeoutConversation))
            {
                if (timeoutConversation.LastAccessed.AddSeconds(TimeoutSeconds) < DateTime.UtcNow)
                {
                    await turnContext.SendActivityAsync("Welcome back!  Let's start over from the beginning.");
                    await DialogState.DeleteAsync(turnContext);
                }
                timeoutConversation.LastAccessed = DateTime.UtcNow;
            }
            else
            {
                var timeoutReference = new TimeoutConversationReference(activity.GetConversationReference());
                _conversationReferences.AddOrUpdate(timeoutReference.ConversationReference.Conversation.Id, timeoutReference, (key, newValue) => timeoutReference);
            }
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await AddOrUpdateConversationReference(turnContext);

            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await Dialog.RunAsync(turnContext, DialogState, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Hi"), cancellationToken);
                }
            }
        }
    }
}
