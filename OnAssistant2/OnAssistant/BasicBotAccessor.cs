using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using OnAssistant.State;

namespace OnAssistant
{
    /// <summary>
    /// This class is created as a Singleton and passed into the IBot-derived constructor.
    ///  - See <see cref="BasicBot"/> constructor for how that is injected.
    ///  - See the Startup.cs file for more details on creating the Singleton that gets
    ///    injected into the constructor.
    /// </summary>
    public class BasicBotAccessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasicBotAccessor"/> class.
        /// Contains the <see cref="ConversationState"/> and associated <see cref="IStatePropertyAccessor{T}"/>.
        /// </summary>
        /// <param name="conversationState">The state object that stores the counter.</param>
        /// <param name="userState">The state object that stores user informations.</param>
        public BasicBotAccessor(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        /// <summary>
        /// Gets or sets the<see cref= "IStatePropertyAccessor{T}" /> for ConversationDialogState.
        /// </summary>
        /// <value>
        /// The accessor stores the dialog state for the conversation.
        /// </summary>
        public IStatePropertyAccessor<DialogState> ConvesationDialogState { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IStatePropertyAccessor{T}"/> for TopicState.
        /// </summary>
        /// <value>
        /// Keep track where we are during dialog.
        /// </value>
        public IStatePropertyAccessor<TopicState> TopicState { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ConversationState"/> object for the conversation.
        /// </summary>
        /// <value>
        /// The <see cref="ConversationState"/> object.
        /// </value>
        public ConversationState ConversationState { get; }

        /// <summary>
        /// Gets or sets the <see cref="UserState"/> object for the conversation.
        /// </summary>
        /// <value>The <see cref="UserState"/> object.</value>
        public UserState UserState { get; set; }
    }
}
