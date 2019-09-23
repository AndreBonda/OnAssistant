using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using OnAssistant.State;
using OnAssistant.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OnAssistant.Utility.BotNames;

namespace OnAssistant.Dialogs.SlotFilling
{
    /// <summary>
    /// Implementazione del dialogo di tipo SlotFilling. Contiene al suo interno una lista di 'slots', dove ognuno dei quali rappresenta un valore che vogliamo raccogliere attraverso l'input dell'utente.
    /// </summary>
    public class SlotFillingDialog : Dialog
    {
        /// <summary>
        /// To comment.
        /// </summary>
        private const string SlotName = "slot";
        private const string PersistedValues = "values";

        // Lista delle proprietà che il dialogo deve collezionare.
        private readonly List<SlotDetails> _slots;

        // Accessors che permette l'accesso a valori memorizzati durante la conversazione.
        private readonly BasicBotAccessor _botAccessors;

        private TopicState _topicState;

        public SlotFillingDialog(string dialogId, BasicBotAccessor botAccessor, List<SlotDetails> slots)
            : base(dialogId)
        {
            _slots = slots ?? throw new ArgumentException(nameof(slots));
            _botAccessors = botAccessor;
        }

        /// <summary>
        /// Metodo chiamato quando il dialogo viene aggiunto allo stack dei dialoghi.
        /// </summary>
        /// <param name="dialogContext">Mantiene lo stato della conversazione.</param>
        /// <param name="options">Utilizzato per passare un parametro già memorizzato.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ritorna DialogTurnResult che indica lo stato di questo dialogo al chiamante.</returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dialogContext, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dialogContext == null)
            {
                throw new ArgumentNullException(nameof(dialogContext));
            }

            if (dialogContext.Context.Activity.Type != ActivityTypes.Message)
            {
                return await dialogContext.EndDialogAsync(new Dictionary<string, object>());
            }

            var state = GetPersistedValues(dialogContext.ActiveDialog);

            // Controllo finchè non trovo uno slot che non è stato memorizzato.
            var unfilledSlot = _slots.FirstOrDefault((item) => !state.ContainsKey(item.Name));

            // Se presente uno slot non memorizzato.
            if (unfilledSlot != null)
            {
                // Nome dello slot non memorizzato che richiediamo all'utente. Verrà recuperato nel metodo ResumeDialogAsync.
                dialogContext.ActiveDialog.State[SlotName] = unfilledSlot.Name;

                // Eseguo il dialogo figlio corrispondente allo slot da memorizzare.
                return await dialogContext.BeginDialogAsync(unfilledSlot.DialogId, unfilledSlot.Options, cancellationToken);
            }
            else
            {
                // Se tutti gli slot sono memorizzati termino il dialogo corrente che verrà rimosso dallo stack.
                return await dialogContext.EndDialogAsync(state);
            }
        }

        /// <summary>
        /// Chiamato per continuare un dialogo esistente nello stack.
        /// </summary>
        /// <param name="dialogContext">Mantiene lo stato della conversazione.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ritorna DialogTurnResult che indica lo stato di questo dialogo al chiamante.</returns>
        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dialogContext == null)
            {
                throw new ArgumentNullException(nameof(dialogContext));
            }

            if (dialogContext.Context.Activity.Type != ActivityTypes.Message)
            {
                return EndOfTurn;
            }

            var state = GetPersistedValues(dialogContext.ActiveDialog);

            // Controllo finchè non trovo uno slot che non è stato memorizzato.
            var unfilledSlot = _slots.FirstOrDefault((item) => !state.ContainsKey(item.Name));

            // Se presente uno slot non memorizzato.
            if (unfilledSlot != null)
            {
                // Nome dello slot non memorizzato che richiediamo all'utente. Verrà recuperato nel metodo ResumeDialogAsync.
                dialogContext.ActiveDialog.State[SlotName] = unfilledSlot.Name;

                // Eseguo il dialogo figlio corrispondente allo slot da memorizzare.
                return await dialogContext.BeginDialogAsync(unfilledSlot.DialogId, unfilledSlot.Options, cancellationToken);
            }
            else
            {
                // Se tutti gli slot sono memorizzati termino il dialogo corrente che verrà rimosso dallo stack.
                return await dialogContext.EndDialogAsync(state);
            }
        }

        /// <summary>
        /// Chiamato quando un dialogo figlio viene completato e necessitiamo di portare avanti il processo in questa classe.
        /// </summary>
        /// <param name="dialogContext">Mantiene lo stato della conversazione.</param>
        /// <param name="reason">.</param>
        /// <param name="result">Risultato ritornato dal dialogo figlio.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ritorna DialogTurnResult che indica lo stato di questo dialogo al chiamante.</returns>
        public override async Task<DialogTurnResult> ResumeDialogAsync(DialogContext dialogContext, DialogReason reason, object result, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dialogContext == null)
            {
                throw new ArgumentNullException(nameof(dialogContext));
            }

            // Aggiorna lo stato con il risultato ottenuto dal dialogo figlio.
            var slotName = (string)dialogContext.ActiveDialog.State[SlotName];
            var values = GetPersistedValues(dialogContext.ActiveDialog);
            _topicState = await _botAccessors.TopicState.GetAsync(dialogContext.Context, () => new TopicState(), cancellationToken);

            ActionCorrectlyIntent();

            // prossimo step della conversazione
            switch (slotName)
            {
                case BotNames.CodiceFiscale:
                    //result = "BNDNDR95H05C573M"; // todo: delete this row
                    var paw = await OnHealthAPI.OnHealthAPI.RecuperaCodicePazienteWeb(result.ToString());
                    if (!string.IsNullOrEmpty(paw))
                    {
                        _topicState.IdPawUser = paw;
                    }
                    else
                    {
                        // Adaptive card se il l'user non è registrato
                        await dialogContext.BeginDialogAsync(BotNames.TextCF, new PromptOptions { Prompt = MessageFactory.Text("Non è stato trovato nessun paziente web collegato al tuo codice fiscale.") }, cancellationToken);
                        string cardPath = Path.Combine(Environment.CurrentDirectory, "Resources", "OnHealthCard.json");
                        var adaptiveCardJson = File.ReadAllText(cardPath);
                        var adaptiveCardAttachment = new Attachment()
                        {
                            ContentType = "application/vnd.microsoft.card.adaptive",
                            Content = JsonConvert.DeserializeObject(adaptiveCardJson),
                        };
                        var reply = dialogContext.Context.Activity.CreateReply();
                        reply.Attachments = new List<Attachment>() { adaptiveCardAttachment };
                        await dialogContext.Context.SendActivityAsync(reply, cancellationToken);
                        TopicState.ResetTopicState(_topicState);
                        return await dialogContext.CancelAllDialogsAsync(cancellationToken);
                    }

                    break;

                case BotNames.CodiceRE:
                    //result = "080123456789120"; // todo: delete this row
                    var res = await OnHealthAPI.OnHealthAPI.RicercaPrestazioniDema(result.ToString(), _topicState.IdPawUser);
                    if (res.PrestazioniEffettive != null && res.PrestazioniEffettive.Count > 0)
                    {
                        string msg = "Ecco le prestazioni previste:\n";
                        _topicState.NRE = result.ToString();
                        _topicState.PrestazioniPrenotabili = res;
                        foreach (var pre in res.PrestazioniEffettive)
                        {
                            msg += "- " + pre.Value + "\n";
                        }

                        await dialogContext.Context.SendActivityAsync(msg);
                    }
                    else
                    {
                        TopicState.ResetTopicState(_topicState);
                        await dialogContext.Context.SendActivityAsync("Non ho trovato nessuna prestazione associata, la prenotazione è stata annullata.");
                        await dialogContext.Context.SendActivityAsync("Vuoi accedere al servizio appuntamenti o alle FAQ?");
                        return await dialogContext.CancelAllDialogsAsync(cancellationToken);
                    }

                    break;

                case BotNames.Orario:
                    return await dialogContext.BeginDialogAsync(BotNames.CercaDisponibilita, cancellationToken);
            }

            values.Add(slotName, result);

            // Controllo finchè non trovo uno slot che non è stato memorizzato.
            var unfilledSlot = _slots.FirstOrDefault((item) => !values.ContainsKey(item.Name));

            // Se presente uno slot non memorizzato.
            if (unfilledSlot != null)
            {
                // Nome dello slot non memorizzato che richiediamo all'utente. Verrà recuperato nel metodo ResumeDialogAsync.
                dialogContext.ActiveDialog.State[SlotName] = unfilledSlot.Name;

                switch (unfilledSlot.Name)
                {
                    case BotNames.Orario:
                        _topicState.NextIntentExpectedByUser = BotNames.IntentOrario;
                        break;
                }

                // Eseguo il dialogo figlio corrispondente allo slot da memorizzare.
                return await dialogContext.BeginDialogAsync(unfilledSlot.DialogId, unfilledSlot.Options, cancellationToken);
            }
            else
            {
                // Se tutti gli slot sono memorizzati termino il dialogo corrente che verrà rimosso dallo stack.
                return await dialogContext.EndDialogAsync(values);
            }
        }

        /// <summary>
        /// Funzione di supporto per gestire i valori persistenti che raccogliamo in questa finestra di dialogo.
        /// </summary>
        /// <param name="dialogInstance">Traccia le informazioni di uno dialogo nello stack.</param>
        /// <returns>Un oggetto IDictionary che rappresente lo stato corrente o un nuovo dizionario se non è ancora stato inizializzato.</returns>
        private static IDictionary<string, object> GetPersistedValues(DialogInstance dialogInstance)
        {
            object obj;
            if (!dialogInstance.State.TryGetValue(PersistedValues, out obj))
            {
                obj = new Dictionary<string, object>();
                dialogInstance.State.Add(PersistedValues, obj);
            }

            return (IDictionary<string, object>)obj;
        }

        /// <summary>
        /// Formatta le entities relative ai giorni per la ricerca delle disponibilità, eliminando il prefisso.
        /// </summary>
        /// <param name="giorno">Giorno settimanale</param>
        /// <returns>Ritorna il giorno settimanale privado del prefisso.</returns>
        private string FormattaGiorno(string giorno)
        {
            string value = giorno;
            if (giorno.StartsWith("da"))
            {
                value = giorno.Substring(2);
            }
            else if (giorno.StartsWith("a"))
            {
                value = giorno.Substring(1);
            }

            return value;
        }

        /// <summary>
        /// Metodo che implementa la logica del dialogo. Utilizza un IDictionary che mantiene lo stato del Dialogo, permettendo il controllo sui valori salvati e non, inseriti dall'utente.
        /// Quando viene trovato un campo non memorizzato si richiede all'user con la rispettiva richiesta (Prompt Dialog).
        /// </summary>
        /// <param name="dialogContext">Mantiene lo stato della conversazione.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Ritorna DialogTurnResult che indica lo stato di questo dialogo al chiamante.</returns>
        private Task<DialogTurnResult> RunPromptAsync(DialogContext dialogContext, CancellationToken cancellationToken)
        {
            var state = GetPersistedValues(dialogContext.ActiveDialog);

            // Controllo finchè non trovo uno slot che non è stato memorizzato.
            var unfilledSlot = _slots.FirstOrDefault((item) => !state.ContainsKey(item.Name));

            // Se presente uno slot non memorizzato.
            if (unfilledSlot != null)
            {
                // Nome dello slot non memorizzato che richiediamo all'utente. Verrà recuperato nel metodo ResumeDialogAsync.
                dialogContext.ActiveDialog.State[SlotName] = unfilledSlot.Name;

                // Eseguo il dialogo figlio corrispondente allo slot da memorizzare.
                return dialogContext.BeginDialogAsync(unfilledSlot.DialogId, unfilledSlot.Options, cancellationToken);
            }
            else
            {
                // Se tutti gli slot sono memorizzati termino il dialogo corrente che verrà rimosso dallo stack.
                return dialogContext.EndDialogAsync(state);
            }
        }

        /// <summary>
        /// Metodo che processa le varie parti del dialogo dove è necessario che l'intent inserito dall'user sia effettivamente quello atteso dal Bot.
        /// </summary>
        private void ActionCorrectlyIntent()
        {
            if (_topicState.NextIntentExpectedByUser == _topicState.LastIntentByUser)
            {
                List<KeyValuePair<string, string>> filtroGiorniFascia = new List<KeyValuePair<string, string>>();
                switch (_topicState.LastIntentByUser)
                {
                    case BotNames.IntentOrario:
                        foreach (var entry in _topicState.LastEntitiesByUser)
                        {
                            if (BotNames.FasciaOrariaEntities.Contains(entry.Key))
                            {
                                foreach (var elem in entry.Value)
                                {
                                    string value = FormattaGiorno(elem);
                                    filtroGiorniFascia.Add(new KeyValuePair<string, string>(entry.Key, value));
                                }
                            }
                        }

                        if (filtroGiorniFascia.Count > 0)
                        {
                            if ((filtroGiorniFascia.Where(x => x.Key == "daGiorno").Count() > 0 && filtroGiorniFascia.Where(x => x.Key == "aGiorno").Count() > 0) || filtroGiorniFascia.Where(x => x.Key == "Giorno").Count() > 0)
                            {
                                _topicState.Filtri.ResetGiorni();

                                // Recupero gli intervalli di tempo
                                if (filtroGiorniFascia.Where(x => x.Key == "daGiorno").Count() > 0 && filtroGiorniFascia.Where(x => x.Key == "aGiorno").Count() > 0)
                                {
                                    string giorno1 = filtroGiorniFascia.First(x => x.Key == "daGiorno").Value;
                                    string giorno2 = filtroGiorniFascia.First(x => x.Key == "aGiorno").Value;
                                    Enum.TryParse(giorno1, out GiornoSettimana giornoPartenza);
                                    Enum.TryParse(giorno2, out GiornoSettimana giornoArrivo);

                                    _topicState.Filtri.ResetGiorni();

                                    if (giornoPartenza < giornoArrivo)
                                    {
                                        _topicState.Filtri.Giorni.Add(giornoPartenza);
                                        _topicState.Filtri.Giorni.Add(giornoArrivo);

                                        foreach (GiornoSettimana gg in Enum.GetValues(typeof(GiornoSettimana)))
                                        {
                                            if (gg > giornoPartenza && gg < giornoArrivo)
                                            {
                                                _topicState.Filtri.Giorni.Add(gg);
                                            }
                                        }
                                    }
                                    else if (giornoPartenza > giornoArrivo)
                                    {
                                        _topicState.Filtri.Giorni.Add(giornoPartenza);
                                        _topicState.Filtri.Giorni.Add(giornoArrivo);

                                        foreach (GiornoSettimana gg in Enum.GetValues(typeof(GiornoSettimana)))
                                        {
                                            if (gg > giornoPartenza || gg < giornoArrivo)
                                            {
                                                _topicState.Filtri.Giorni.Add(gg);
                                            }
                                        }
                                    }
                                    else if (giornoPartenza == giornoArrivo)
                                    {
                                        _topicState.Filtri.Giorni.Add(giornoPartenza);
                                    }
                                }

                                // recupero i giorni non compresi in un intervallo
                                if (filtroGiorniFascia.Where(x => x.Key == "Giorno").Count() > 0)
                                {
                                    foreach (var entry in filtroGiorniFascia.Where(x => x.Key == "Giorno"))
                                    {
                                        Enum.TryParse(entry.Value, out GiornoSettimana giorno);
                                        if (!_topicState.Filtri.Giorni.Contains(giorno))
                                        {
                                            _topicState.Filtri.Giorni.Add(giorno);
                                        }
                                    }
                                }
                            }

                            // recupero fascia giornaliera
                            if (filtroGiorniFascia.Where(x => x.Key == "fasciaOra").Count() > 0)
                            {
                                _topicState.Filtri.ResetFasce();
                                foreach (var entry in filtroGiorniFascia.Where(x => x.Key == "fasciaOra"))
                                {
                                    Enum.TryParse(entry.Value, out FasciaGiorno fascia);

                                    if (fascia == FasciaGiorno.mattina)
                                    {
                                        _topicState.Filtri.Mattina = true;
                                    }
                                    else if (fascia == FasciaGiorno.pomeriggio)
                                    {
                                        _topicState.Filtri.Pomeriggio = true;
                                    }
                                }
                            }

                            foreach (var giorno in _topicState.Filtri.Giorni)
                            {
                                switch (giorno)
                                {
                                    case GiornoSettimana.Lunedi:
                                        _topicState.Filtri.Lunedi = true;
                                        break;

                                    case GiornoSettimana.Martedi:
                                        _topicState.Filtri.Martedi = true;
                                        break;

                                    case GiornoSettimana.Mercoledi:
                                        _topicState.Filtri.Mercoledi = true;
                                        break;

                                    case GiornoSettimana.Giovedi:
                                        _topicState.Filtri.Giovedi = true;
                                        break;

                                    case GiornoSettimana.Venerdi:
                                        _topicState.Filtri.Venerdi = true;
                                        break;

                                    case GiornoSettimana.Sabato:
                                        _topicState.Filtri.Sabato = true;
                                        break;

                                    case GiornoSettimana.Domenica:
                                        _topicState.Filtri.Domenica = true;
                                        break;
                                }
                            }
                        }

                        _topicState.LastEntitiesByUser = new Dictionary<string, List<string>>();
                        break;
                }
            }
        }
    }
}
