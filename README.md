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
