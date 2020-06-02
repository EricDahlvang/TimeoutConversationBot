using System;
using Microsoft.Bot.Schema;

namespace MultiTurnPromptBot
{
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
}
