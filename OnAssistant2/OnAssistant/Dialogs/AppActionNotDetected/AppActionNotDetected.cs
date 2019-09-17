using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using OnAssistant.State;
using OnAssistant.Utility;

namespace OnAssistant.Dialogs.AppActionNotDetected
{
    public class AppActionNotDetected : Dialog
    {

        private BasicBotAccessor _botAccessor;
        private TopicState _topicState;

        public AppActionNotDetected(string dialogId, BasicBotAccessor botAccessor)
            : base(dialogId)
        {
            _botAccessor = botAccessor;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dc.BeginDialogAsync("text", new PromptOptions { Prompt = MessageFactory.Text("Non ho capito quale operazione desideri fare. Puoi prenotare un nuovo appuntamento oppure modificare o cancellare uno già esistente.") }, cancellationToken);
        }

        public override Task EndDialogAsync(ITurnContext turnContext, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.EndDialogAsync(turnContext, instance, reason, cancellationToken);
        }

        public override async Task<DialogTurnResult> ResumeDialogAsync(DialogContext dc, DialogReason reason, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            _topicState = await _botAccessor.TopicState.GetAsync(dc.Context, () => new TopicState(), cancellationToken);
            string value;
            var entities = _topicState.LastEntitiesByUser.FirstOrDefault(p => p.Key == BotNames.EntityPrenotazione);
            if (entities.Value != null && entities.Value.Count() > 0)
            {
                value = entities.Value.First();
                switch (value)
                {
                    case BotNames.EntityNuovoApp:
                        return await dc.ReplaceDialogAsync("nuovo_app_dialogo", null, cancellationToken);

                    case BotNames.EntitySpostaApp:
                        return await dc.ReplaceDialogAsync("modifica_app_dialogo", null, cancellationToken);

                    case BotNames.EntityCancellaApp:
                        return await dc.ReplaceDialogAsync("modifica_app_dialogo", null, cancellationToken);

                    default:
                        return null;
                }
            }
            else
            {
                return await dc.ReplaceDialogAsync("app_action_not_detected", null, cancellationToken);
            }
        }
    }
}
