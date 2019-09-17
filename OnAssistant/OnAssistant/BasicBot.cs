// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OnAssistant.Dialogs;
using OnAssistant.Dialogs.AppActionNotDetected;
using OnAssistant.Dialogs.CercaDisponibilita;
using OnAssistant.Dialogs.SlotFilling;
using OnAssistant.State;
using OnAssistant.Utility;

namespace OnAssistant
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class BasicBot : IBot
    {
        private static int _numCF = 16;
        private static int _numRE = 15;
        private readonly UserState _userState;
        private readonly BasicBotAccessor _botAccessors;
        private TopicState topicState;
        private BotServices _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicBot"/> class.
        /// </summary>
        /// <param name="services">Bot services.</param>
        /// <param name="botAccessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage conversation & user state.</param>
        public BasicBot(BotServices services, BasicBotAccessor botAccessors)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _botAccessors = botAccessors ?? throw new System.ArgumentNullException(nameof(botAccessors));
            Dialogs = new DialogSet(_botAccessors.ConvesationDialogState);

            if (!_services.LuisServices.ContainsKey(BotNames.LuisPrenotazione))
            {
                throw new System.ArgumentException($"Invalid configuration.  Please check your '.bot' file for a Luis service named '{BotNames.LuisPrenotazione}'.");
            }

            if (!_services.LuisServices.ContainsKey(BotNames.LuisRefertazione))
            {
                throw new System.ArgumentException($"Invalid configuration.  Please check your '.bot' file for a Luis service named '{BotNames.LuisRefertazione}'.");
            }

            var new_app = new List<SlotDetails>
            {
                new SlotDetails(BotNames.CodiceFiscale, BotNames.TextCF, "Inserisci il tuo codice fiscale (solo i 16 caratteri)", "Codice fiscale non corretto! Riprovare."),
                new SlotDetails(BotNames.CodiceRE, BotNames.TextRE, "Inserire il codice della ricetta elettronica (solo le 15 cifre)", "Codice richiesta elettronica non corretto! Riprovare."),
                new SlotDetails(BotNames.Orario, BotNames.Text, "Se desideri, puoi inserire i giorni della settimana e la fascia oraria (mattina o pomeriggio) che preferisci."),
            };

            Dialogs.Add(new SlotFillingDialog(BotNames.NewAppDialog, _botAccessors, new_app));
            Dialogs.Add(new TextPrompt(BotNames.TextCF, ValidationCF));
            Dialogs.Add(new TextPrompt(BotNames.TextRE, ValidationRE));
            Dialogs.Add(new TextPrompt(BotNames.Text));
            Dialogs.Add(new AppActionNotDetected(BotNames.AppActionNotDetected, _botAccessors));
            Dialogs.Add(new CercaDisponibilitaDialog(BotNames.CercaDisponibilita, _botAccessors));
            Dialogs.Add(new WaterfallDialog(BotNames.Root, new WaterfallStep[] { StartDialogAsync, ProcessResultAsync }));
        }

        /// <summary>
        /// Gets or sets dialogs <see cref="DialogSet"/> that contains all the Dialogs that can be used at runtime.
        /// </summary>
        private DialogSet Dialogs { get; set; }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = turnContext.Activity;

            if (activity.Type == ActivityTypes.Message)
            {
                topicState = await _botAccessors.TopicState.GetAsync(turnContext, () => new TopicState(), cancellationToken);

                // configurazione del modulo LUIS in uso.
                var luisActualConfig = topicState.LuisModuleInUse;

                // Perform a call to LUIS to retrieve results for the current activity message
                var recognizerResult = await _services.LuisServices[luisActualConfig].RecognizeAsync(turnContext, cancellationToken);
                var topIntent = recognizerResult?.GetTopScoringIntent();
                topicState.LastIntentByUser = topIntent.Value.intent;
                SetEntities(recognizerResult.Entities, topicState);

                switch (luisActualConfig)
                {
                    case BotNames.LuisDispatch:
                        // se il modulo in uso è il dispatch, siamo nella root della conversazione e devo impostare il modulo a seconda del primo input dell'utente.
                        await DispatchNextStep(turnContext, topicState, topIntent, cancellationToken);
                        break;

                    case BotNames.LuisPrenotazione:
                        await PrenotazioneNextStep(turnContext, topicState, topIntent, cancellationToken);
                        break;

                    case BotNames.LuisRefertazione:
                        break;
                }
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded != null)
                {
                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }

            // Salvataggio dello stato della conversazione
            await _botAccessors.ConversationState.SaveChangesAsync(turnContext);

            // decommentare nel caso si aggiungano info che debbano persistere tra conversazioni multiple (es: nome utente)
            // await _botAccessors.UserState.SaveChangesAsync(turnContext);
        }

        private async Task<DialogTurnResult> StartDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string value = string.Empty;

            // recupero le entita relative relative all'intent di prenotazione
            var entities = topicState.LastEntitiesByUser.FirstOrDefault(p => p.Key == BotNames.EntityPrenotazione);
            if (entities.Value != null && entities.Value.Count() > 0)
            {
                value = entities.Value.First();
            }

            if (value == BotNames.EntityNuovoApp)
            {
                return await stepContext.BeginDialogAsync(BotNames.NewAppDialog, null, cancellationToken);
            }
            else if (value == BotNames.EntitySpostaApp)
            {
                await stepContext.Context.SendActivityAsync("Mi dispiace ma attualmente non sono in grado di spostare un appuntamento.");
                await stepContext.Context.SendActivityAsync("Vuoi prenotare un nuovo appuntamento o accedere al servizio FAQ?");
                TopicState.ResetTopicState(topicState);
                return await stepContext.CancelAllDialogsAsync(cancellationToken);
            }
            else if (value == BotNames.EntityCancellaApp)
            {
                return await stepContext.BeginDialogAsync(BotNames.CancelAppDialog, null, cancellationToken);
            }
            else
            {
                // Se nessuna entità è rilevata inizio di un dialogo apposito che va in "loop" finchè non viene inserito correttamente
                return await stepContext.BeginDialogAsync(BotNames.AppActionNotDetected, null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ProcessResultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //var result = (IDictionary<string, object>)stepContext.Result;
            //result.TryGetValue("nome", out var name);
            //result.TryGetValue("cognome", out var surname);

            TopicState.ResetTopicState(topicState);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Prenotazione avvenuta con successo!"), cancellationToken);
            // runtime è terminato fuori dal dialogo waterfall.
            return await stepContext.EndDialogAsync();
        }

        /// <summary>
        /// Setta le entità in topicState.
        /// </summary>
        /// <param name="entities">Lista di entità rilevate da LUIS.ai.</param>
        /// <param name="topicState">Campo del botAccessor che memorizza gli ultimi inserimenti dell'utente.</param>
        private void SetEntities(JObject entities, TopicState topicState)
        {
            if (entities.Count > 1 && entities != null)
            {
                foreach (var entity in topicState.LuisEntitiesInUse)
                {
                    var values = entities[entity];

                    if (values != null && values.Count() > 0)
                    {
                        List<string> list = new List<string>();
                        foreach (var val in values)
                        {
                            list.Add(val.First.ToString());
                        }

                        topicState.LastEntitiesByUser.Add(entity, list);
                    }
                }
            }
        }

        /// <summary>
        /// Metodo chiamato che setta il prossimo step se il bot sta eseguendo il dialogo di prenotazione.
        /// </summary>
        /// <param name="turnContext">turnContext.</param>
        /// <param name="topicState">topicState.</param>
        /// <param name="topIntent">topIntent inserito dall'utente.</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        private async Task PrenotazioneNextStep(ITurnContext turnContext, TopicState topicState, (string intent, double score)? topIntent, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Eseguo DialogSet. Il framework identifica lo stato corrente del dialogo attivo (ActiveDialog) controllando lo stack.
            var dialogContext = await Dialogs.CreateContextAsync(turnContext, cancellationToken);
            var results = await dialogContext.ContinueDialogAsync(cancellationToken);

            // Se lo stack è vuoto, analizza la richiesta dell'utente (aggiungere, modificare o cancellare) e aggiungere il dialogo rispettivo.
            if (results.Status == DialogTurnStatus.Empty)
            {
                await dialogContext.BeginDialogAsync(BotNames.Root, null, cancellationToken);
            }
            else if (results.Status == DialogTurnStatus.Cancelled)
            {

                await DispatchNextStep(turnContext, topicState, topIntent, cancellationToken);
            }
        }

        /// <summary>
        /// Metodo chiamato che setta il prossimo step se il bot si trova nella root.
        /// </summary>
        /// <param name="turnContext">turnContext.</param>
        /// <param name="topicState">topicState.</param>
        /// <param name="topIntent">topIntent inserito dall'utente.</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        private async Task DispatchNextStep(ITurnContext turnContext, TopicState topicState, (string intent, double score)? topIntent, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topicState.RestartConversation)
            {
                topicState.RestartConversation = false;
                await turnContext.SendActivityAsync("Vuoi prenotare un nuovo appuntamento o accedere al servizio FAQ?");
            }
            else
            {
                if (topIntent == null)
                {
                    await turnContext.SendActivityAsync("Unable to get the top intent.");
                }
                else
                {
                    await DispatchToTopIntentAsync(turnContext, topicState, topIntent, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Configurazione del Topic state in base all'intent inserito dall'utente.
        /// </summary>
        /// <param name="turnContext">turnContext.</param>
        /// <param name="topicState">topicState.</param>
        /// <param name="topIntent">topIntent inserito dall'utente.</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        private async Task DispatchToTopIntentAsync(ITurnContext context, TopicState topicState, (string intent, double score)? topIntent, CancellationToken cancellationToken = default(CancellationToken))
        {
            const string prenotazioneDispatchIntent = "l_OnAssistant-prenotazione";
            const string refertazioneDispatchIntent = "l_OnAssistant-refertazione";
            const string noneDispatchIntent = "None";

            switch (topIntent.Value.intent)
            {
                case prenotazioneDispatchIntent:
                    await context.SendActivityAsync("Certo, quale operazioni desideri effettuare?");
                    topicState.LuisModuleInUse = BotNames.LuisPrenotazione;
                    topicState.LuisEntitiesInUse = BotNames.PrenotationEntities;
                    break;

                case refertazioneDispatchIntent:
                    await context.SendActivityAsync("Ecco i tuoi referti medici dell'ultimo anno. Se vuoi puoi raffinare la tua ricerca! (da implementare)");
                    topicState.LuisModuleInUse = BotNames.LuisRefertazione;
                    break;

                case noneDispatchIntent:
                    await context.SendActivityAsync("Cosa vuoi fare? Puoi prenotare un nuovo appuntamento, o accedere al servizio FAQ.");

                    break;
            }
        }

        /// <summary>
        /// A fronte di una attività di tipo ConversationUpdate, il bot manda un messaggio ad ogni nuovo utente/utenti aggiunto/i.
        /// </summary>
        /// <param name="turnContext">Provides the <see cref="ITurnContext"/> for the turn of the bot.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        private async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            string welcomeMsg = "Salve, sono OnAssistant e sono il suo assistente sanitario. Posso gestire i tuoi appuntamenti, o accedere al servizio FAQ. Cosa desideri fare?";
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(welcomeMsg, cancellationToken: cancellationToken);
                }
            }
        }

        /// <summary>
        /// Controlla se l'input inserito dall'utente corrisponde ad un codice fiscale e memorizza in topic_state il codice del paziente web su OnHealth.
        /// </summary>
        /// <param name="promt">Prompt dell'utente</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ritorna un task asincrono di tipo bool che indica il successo o meno della validazione.</returns>
        private Task<bool> ValidationCF(PromptValidatorContext<string> promt, CancellationToken cancellationToken)
        {
            var cf = promt.Recognized.Value.Trim();

            return Task.FromResult(true);
            // return (!string.IsNullOrEmpty(cf) && cf.Count() == _numCF);
        }

        /// <summary>
        /// Controlla se l'input inserito dall'utente corrisponde ad una ricetta elettronica.
        /// </summary>
        /// <param name="prompt">Prompt dell'utente</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ritorna un task asincrono di tipo bool che indica il successo o meno della validazione.</returns>
        private Task<bool> ValidationRE(PromptValidatorContext<string> prompt, CancellationToken cancellationToken)
        {
            var codRE = prompt.Recognized.Value.Trim();

            return Task.FromResult(true);

            //if (codRE.Count() == _numRE)
            //{
            //return await Task.FromResult(true);
            //}

            //return await Task.FromResult(false);
        }
    }
}
