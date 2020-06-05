See also: https://github.com/EricDahlvang/TimeoutConversation  for an Aure Function method of accomplishing something similar, in a proactive way.


# TimeoutConversationBot

Modified https://github.com/microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/05.multi-turn-prompt 

A concurrent dictionary of TimeoutConversationReference, keyed on Conversation.Id, is used to track last access time.  This could also be done with any persistent storage provider.

```cs
    public class TimeoutConversationReference
    {
        public TimeoutConversationReference(ConversationReference conversationReference)
        {
            this.ConversationReference = conversationReference;
            this.LastAccessed = DateTime.UtcNow;
        }

        public DateTime LastAccessed { get; set; }

        public ConversationReference ConversationReference { get; set; }
    }
```

When an activity is received by the bot, the concurrent dictionary is checked for an existing TimeoutConversationReference.  If one is present, the LastAccessed is compared to the current time and DialogState is cleared if the conversation has expired.

```cs
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
```
