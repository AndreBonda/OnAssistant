using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using OnAssistant.State;
using OnAssistant.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OnAssistant.OnHealthAPI.OnHealthAPI;
using static OnAssistant.OnHealthAPI.OnHealthAPI.RicercaDisponibilitaDTO;

namespace OnAssistant.Dialogs.CercaDisponibilita
{
    public class CercaDisponibilitaDialog : Dialog
    {

        private BasicBotAccessor _botAccessor;
        private TopicState _topicState;

        public CercaDisponibilitaDialog(string dialogId, BasicBotAccessor botAccessor)
            : base(dialogId)
        {
            _botAccessor = botAccessor;
        }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            _topicState = await _botAccessor.TopicState.GetAsync(dc.Context, () => new TopicState(), cancellationToken);
            RicercaDisponibilitaDTO disponibilita = null;

            // ricerca disponibilità
            disponibilita = await OnHealthAPI.OnHealthAPI.RicercaDisponibilita(_topicState.NRE, _topicState.IdPawUser, _topicState.PrestazioniPrenotabili.PrestazioniEffettive.ElementAt(_topicState.ContatorePrestazioneInPrenotazione).Key, _topicState.Filtri);
            if (disponibilita != null && disponibilita.Proposte.Count() > 0 && disponibilita.TipoMessaggio == MessageResultType.Success)
            {
                _topicState.Disponibilita = disponibilita;
                await dc.Context.SendActivityAsync("Ecco la prima disponibiltà in base ai tuoi criteri di ricerca: \n" + GeneraMessaggioDisponibilita(disponibilita.Proposte.First(), _topicState.ContatoreOrari));
                return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Desideri fermare questa disponibilità?") });
            }
            else
            {
                _topicState.ContatorePrestazioneInPrenotazione += 1;
                if (_topicState.ContatorePrestazioneInPrenotazione >= _topicState.PrestazioniPrenotabili.PrestazioniEffettive.Count)
                {
                    // fine prestazioni
                    TopicState.ResetTopicState(_topicState);
                    await dc.Context.SendActivityAsync("Le prestazioni da prenotare sono terminate.");
                    await dc.Context.SendActivityAsync("Vuoi prenotare un nuovo appuntamento o accedere al servizio FAQ?");
                    return await dc.EndDialogAsync(dc, cancellationToken);
                }
                else
                {
                    // presente almeno un'altra prestazione da prenotare
                    return await ContinueDialogAsync(dc, cancellationToken);
                }
            }
        }

        public async override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            _topicState = await _botAccessor.TopicState.GetAsync(dc.Context, () => new TopicState(), cancellationToken);
            RicercaDisponibilitaDTO disponibilita = null;

            // ricerca disponibilità
            disponibilita = await OnHealthAPI.OnHealthAPI.RicercaDisponibilita(_topicState.NRE, _topicState.IdPawUser, _topicState.PrestazioniPrenotabili.PrestazioniEffettive.ElementAt(_topicState.ContatorePrestazioneInPrenotazione).Key, _topicState.Filtri);
            if (disponibilita != null && disponibilita.Proposte.Count() > 0 && disponibilita.TipoMessaggio == MessageResultType.Success)
            {
                _topicState.Disponibilita = disponibilita;
                await dc.Context.SendActivityAsync("Disponibilità: \n" + GeneraMessaggioDisponibilita(disponibilita.Proposte.First(), _topicState.ContatoreOrari));
                return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Desideri fermare questa disponibilità?") });
            }
            else
            {
                _topicState.ContatorePrestazioneInPrenotazione += 1;
                if (_topicState.ContatorePrestazioneInPrenotazione >= _topicState.PrestazioniPrenotabili.PrestazioniEffettive.Count)
                {
                    // fine prestazioni
                    TopicState.ResetTopicState(_topicState);
                    await dc.Context.SendActivityAsync("Le prestazioni sono terminate.");
                    await dc.Context.SendActivityAsync("Vuoi prenotare un nuovo appuntamento o accedere al servizio FAQ?");
                    return await dc.CancelAllDialogsAsync(cancellationToken);
                }
                else
                {
                    // presente almeno un'altra prestazione da prenotare
                    return await ContinueDialogAsync(dc, cancellationToken);
                }

            }
        }

        public async override Task<DialogTurnResult> ResumeDialogAsync(DialogContext dc, DialogReason reason, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            _topicState = await _botAccessor.TopicState.GetAsync(dc.Context, () => new TopicState(), cancellationToken);

            if (_topicState.VediProssimaPrestazione)
            {
                switch (_topicState.LastIntentByUser)
                {
                    case BotNames.IntentConferma:
                        _topicState.ContatorePrestazioneInPrenotazione += 1;

                        if (_topicState.ContatorePrestazioneInPrenotazione >= _topicState.PrestazioniPrenotabili.PrestazioniEffettive.Count)
                        {
                            TopicState.ResetTopicState(_topicState);
                            await dc.Context.SendActivityAsync("Le prestazioni da prenotare sono terminate.");
                            return await dc.CancelAllDialogsAsync(cancellationToken);
                        }
                        else
                        {
                            // incremento contatore e si inizia dalla prestazione successiva
                            _topicState.VediProssimaPrestazione = false;
                            return await ContinueDialogAsync(dc, cancellationToken);
                        }

                    default:
                        // se non detecto intent riparto dalla stessa prestazione
                        _topicState.VediProssimaPrestazione = false;
                        return await ContinueDialogAsync(dc, cancellationToken);
                }
            }
            else
            {
                switch (_topicState.LastIntentByUser)
                {
                    case BotNames.IntentConferma:
                        return await SelezionaDisponibilita(dc, cancellationToken);

                    case BotNames.IntentNega:
                        return await NextProposta(dc, cancellationToken);

                    case BotNames.IntentAvanti:
                        return await NextProposta(dc, cancellationToken);

                    case BotNames.IntentIndietro:
                        return await PrevProposta(dc, cancellationToken);

                    default:
                        // se non detecto l'intent mostro la stessa disponibilià
                        await dc.Context.SendActivityAsync("Non ho capito. Disponibilità: \n" + GeneraMessaggioDisponibilita(_topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari));
                        return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Desideri fermare questa disponibilità?") });
                }
            }
        }

        private async Task<DialogTurnResult> NextProposta(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            _topicState.ContatoreOrari += 1;
            if (_topicState.ContatoreOrari >= _topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte).Orari.Count())
            {
                // resetto il contatore degli orari e vado alla proposta successiva.
                _topicState.ContatoreOrari = 0;
                try
                {
                    _topicState.ContatoreProposte += 1;

                    // se proposte terminate va in exception
                    _topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte);

                    // mostro la prossima proposta
                    await dc.Context.SendActivityAsync("Disponibilità: \n" + GeneraMessaggioDisponibilita(_topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari));
                    return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Desideri fermare questa disponibilità?") });
                }
                catch (ArgumentOutOfRangeException)
                {
                    // proposte terminate. Chiedere all'utente se passare alla prestazione successiva o ricominciare a visualizzare le proposte della prestazione attuale.
                    _topicState.VediProssimaPrestazione = true;
                    return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Disponibilità per questa prestazione terminate. Desideri passare alla prossima prestazione?") });
                }
            }
            else
            {
                // mostro la stessa proposta ma nell'orario succesivo disponibile.
                await dc.Context.SendActivityAsync("Disponibilità: \n" + GeneraMessaggioDisponibilita(_topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari));
                return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Desideri fermare questa disponibilità?") });
            }
        }

        private async Task<DialogTurnResult> PrevProposta(DialogContext dc, CancellationToken cancellation = default(CancellationToken))
        {
            _topicState.ContatoreOrari -= 1;

            if (_topicState.ContatoreOrari >= 0)
            {
                await dc.Context.SendActivityAsync("Disponibilità: \n" + GeneraMessaggioDisponibilita(_topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari));
                return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Desideri fermare questa disponibilità?") });
            }
            else
            {
                _topicState.ContatoreProposte -= 1;
                try
                {
                    // se l'orario è il minore della prima proposta va in exception
                    var proposta = _topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte);
                    _topicState.ContatoreOrari = proposta.Orari.Count() - 1;
                    await dc.Context.SendActivityAsync("Disponibilità: \n" + GeneraMessaggioDisponibilita(_topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari));
                    return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Desideri fermare questa disponibilità?") });

                }
                catch (ArgumentOutOfRangeException)
                {
                    _topicState.ContatoreProposte = 0;
                    _topicState.ContatoreOrari = 0;
                    await dc.Context.SendActivityAsync("Non è presente nessuna disponibilità precedente. \n Disponibilità: \n" + GeneraMessaggioDisponibilita(_topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari));
                    return await dc.BeginDialogAsync(BotNames.Text, new PromptOptions { Prompt = MessageFactory.Text("Desideri fermare questa disponibilità?") });
                }
            }
        }

        private async Task<DialogTurnResult> SelezionaDisponibilita(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            RicercaDisponibilitaDTO disponibilitaRichiesta = null;
            BloccaDisponibilitaDTO disponibilitaBloccata = null;
            AppuntamentoConfermatoDTO appuntamento = null;

            string idPrestazione = _topicState.PrestazioniPrenotabili.PrestazioniEffettive.ElementAt(_topicState.ContatorePrestazioneInPrenotazione).Key;
            disponibilitaRichiesta = await OnHealthAPI.OnHealthAPI.SelezionaDisponibilita(_topicState.NRE, _topicState.IdPawUser, idPrestazione, _topicState.Disponibilita.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari);
            if (disponibilitaRichiesta != null && disponibilitaRichiesta.Proposte.Count() == 1 && disponibilitaRichiesta.TipoMessaggio == MessageResultType.Success)
            {
                _topicState.ContatoreOrari = 0;
                disponibilitaBloccata = await OnHealthAPI.OnHealthAPI.BloccaDisponibilita(_topicState.NRE, _topicState.IdPawUser, idPrestazione, disponibilitaRichiesta.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari);
                if (disponibilitaBloccata != null && disponibilitaBloccata.TipoMessaggio == MessageResultType.Success && !string.IsNullOrEmpty(disponibilitaBloccata.IdBlocco))
                {
                    // blocco disponibilita riuscito.
                    appuntamento = await OnHealthAPI.OnHealthAPI.ConfermaAppuntamento(_topicState.IdPawUser, idPrestazione, disponibilitaRichiesta.Proposte.ElementAt(_topicState.ContatoreProposte), _topicState.ContatoreOrari, disponibilitaBloccata.IdBlocco);
                    if (appuntamento != null && appuntamento.TipoMessaggio == MessageResultType.Success && !string.IsNullOrEmpty(appuntamento.Id))
                    {
                        TopicState.ResetTopicState(_topicState);
                        await dc.Context.SendActivityAsync("Appuntamento prenotato correttamente!");
                        return await dc.CancelAllDialogsAsync(cancellationToken);
                    }
                    else
                    {
                        _topicState.ContatoreOrari = 0;
                        await dc.Context.SendActivityAsync("Mi dispiace c'è stato un problema. Non sono riuscito a prenotare l'appuntamento. Ricarico le disponibilità.");
                        return await ContinueDialogAsync(dc, cancellationToken);
                    }
                }
                else
                {
                    // blocco disponibilita non riuscito
                    _topicState.ContatoreOrari = 0;
                    await dc.Context.SendActivityAsync("Mi dispiace ma questa disponibilità non è più disponibile. Ricarico le disponibilità.");
                    return await ContinueDialogAsync(dc, cancellationToken);
                }
            }
            else
            {
                // se la disponibilità selezionata non è più esistente
                _topicState.ContatoreOrari = 0;
                await dc.Context.SendActivityAsync("Mi dispiace ma questa disponibilità non è più disponibile. Ricarico le disponibilità");
                return await ContinueDialogAsync(dc, cancellationToken);
            }
        }

        /// <summary>
        /// Metodo che formatta le info. di una prestazione per essere visualizzate correttamente tramite messaggio.
        /// </summary>
        /// <param name="proposta">Proposta in oggetto.</param>
        /// <param name="contatoreOrario">Contatore orario.</param>
        private string GeneraMessaggioDisponibilita(PropostaAppuntamento_DTO proposta, int contatoreOrario)
        {
            string msg = "Sede: " + proposta.Sede + "\n";
            if (proposta.Data.HasValue)
            {
                msg += "Data: " + proposta.Data.Value.ToShortDateString() + "\n";
            }

            if (proposta.Orari != null && proposta.Orari.Count() > 0)
            {
                msg += proposta.Orari.ToList().ElementAt(contatoreOrario).ToShortTimeString() + "\n";
            }

            msg += "Prezzo: " + proposta.Prezzo;
            return msg;
        }

        /// <summary>
        /// Resetta lo stato della conversazione.
        /// </summary>
        private void ResetTopicState(TopicState topicState)
        {
            _topicState.ContatoreOrari = 0;
            _topicState.ContatorePrestazioneInPrenotazione = 0;
            _topicState.ContatoreProposte = 0;
            _topicState.Filtri = new FiltriGiorniFascia();
            _topicState.IdPawUser = string.Empty;
            _topicState.LastEntitiesByUser = new Dictionary<string, List<string>>();
            _topicState.LastIntentByUser = string.Empty;
            _topicState.Disponibilita = new RicercaDisponibilitaDTO();
            _topicState.LuisEntitiesInUse = null;
            _topicState.LuisModuleInUse = BotNames.LuisDispatch;
            _topicState.NextIntentExpectedByUser = string.Empty;
            _topicState.NRE = string.Empty;
            _topicState.PrestazioniPrenotabili = new PrestazioniDTO();
            _topicState.VediProssimaPrestazione = false;
            _topicState.RestartConversation = true;
        }
    }
}
