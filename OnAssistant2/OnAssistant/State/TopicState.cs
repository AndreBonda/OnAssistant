using OnAssistant.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OnAssistant.OnHealthAPI.OnHealthAPI;
using static OnAssistant.Utility.BotNames;

namespace OnAssistant.State
{
    /// <summary>
    /// Stores state for the conversation.
    /// Stored in <see cref="Microsoft.Bot.Builder.ConversationState"/> and
    /// backed by <see cref="Microsoft.Bot.Builder.MemoryStorage"/>.
    /// </summary>
    public class TopicState
    {
        public TopicState()
        {
            LuisModuleInUse = BotNames.LuisDispatch;
            LastEntitiesByUser = new Dictionary<string, List<string>>();
            FiltroGiorniFasciaOra = new List<KeyValuePair<string, string>>();
            Filtri = new FiltriGiorniFascia();
        }

        /// <summary>
        /// Gets or sets il modulo LUIS in uso, utile per gestire un set di intent ristretto a seconda del modulo in uso.
        /// Es: durante la prenotazione non ha senso che il bot gestisca l'intent per la visualizzazione dei referti.
        /// </summary>
        public string LuisModuleInUse { get; set; }

        /// <summary>
        /// Gets or sets entità in uso, le quali variano a seconda del modulo LUIS attuale.
        /// </summary>
        public string[] LuisEntitiesInUse { get; set; } = null;

        /// <summary>
        /// Gets or sets ultimo intent dell'utente.
        /// </summary>
        public string LastIntentByUser { get; set; }

        /// <summary>
        /// Gets or sets il prossimo intent che il bot si aspetta di ricevere dall'utente.
        /// </summary>
        public string NextIntentExpectedByUser { get; set; }

        /// <summary>
        /// Gets or sets ultime entità dell'utente.
        /// </summary>
        public Dictionary<string, List<string>> LastEntitiesByUser { get; set; }

        /// <summary>
        /// Gets or sets i filtri relativi al giorno e fascia oraria per la ricerca delle disponibilita.
        /// </summary>
        public List<KeyValuePair<string, string>> FiltroGiorniFasciaOra { get; set; }

        /// <summary>
        /// Gets or sets l'id del paziente web dell'utente.
        /// </summary>
        public string IdPawUser { get; set; }

        /// <summary>
        /// Gets or sets il codice della ricetta elettronica.
        /// </summary>
        public string NRE { get; set; }

        /// <summary>
        /// Gets or sets la lista delle prestazioni di una ricetta dema prenotabili.
        /// </summary>
        public PrestazioniDTO PrestazioniPrenotabili { get; set; }

        /// <summary>
        /// Gest or sets i filtri per la ricerca delle disponibilita.
        /// </summary>
        public FiltriGiorniFascia Filtri { get; set; }

        /// <summary>
        /// Gets or sets la lista delle disponibilità relative alla prenotazione della prestazione corrente.
        /// </summary>
        public RicercaDisponibilitaDTO Disponibilita { get; set; }

        /// <summary>
        /// Gets or sets che tiene traccia dell'orario della prestazione in fase di prenotazione.
        /// </summary>
        public int ContatorePrestazioneInPrenotazione { get; set; }

        /// <summary>
        /// Gets ore sets il contatore che tiene traccia delle proposte relative alla ricerca di una disponibilità.
        /// </summary>
        public int ContatoreProposte { get; set; }

        /// <summary>
        /// Gets or sets il contatore che tiene traccia dell'orario selezionato all'interno di una proposta.
        /// </summary>
        public int ContatoreOrari { get; set; }

        /// <summary>
        /// Gest or sets il flag che chiede all'utente se ricominciare a visualizzare le disponibilità della prestazione corrente.
        /// </summary>
        public bool VediProssimaPrestazione { get; set; }

        public bool RestartConversation { get; set; }

        /// <summary>
        /// Metodo per reset del topicState
        /// </summary>
        public static void ResetTopicState(TopicState topicState)
        {
            topicState.ContatoreOrari = 0;
            topicState.ContatorePrestazioneInPrenotazione = 0;
            topicState.ContatoreProposte = 0;
            topicState.Filtri = new FiltriGiorniFascia();
            topicState.IdPawUser = string.Empty;
            topicState.LastEntitiesByUser = new Dictionary<string, List<string>>();
            topicState.LastIntentByUser = string.Empty;
            topicState.Disponibilita = new RicercaDisponibilitaDTO();
            topicState.LuisEntitiesInUse = null;
            topicState.LuisModuleInUse = BotNames.LuisDispatch;
            topicState.NextIntentExpectedByUser = string.Empty;
            topicState.NRE = string.Empty;
            topicState.PrestazioniPrenotabili = new PrestazioniDTO();
            topicState.VediProssimaPrestazione = false;
            topicState.RestartConversation = true;
        }
    }

    public class FiltriGiorniFascia
    {
        public FiltriGiorniFascia()
        {
            Giorni = new List<GiornoSettimana>();
            Lunedi = true;
            Martedi = true;
            Mercoledi = true;
            Giovedi = true;
            Venerdi = true;
            Sabato = true;
            Domenica = true;
            Mattina = true;
            Pomeriggio = true;
        }

        public List<GiornoSettimana> Giorni { get; set; }

        public bool Mattina { get; set; }

        public bool Pomeriggio { get; set; }

        public bool Lunedi { get; set; }

        public bool Martedi { get; set; }

        public bool Mercoledi { get; set; }

        public bool Giovedi { get; set; }

        public bool Venerdi { get; set; }

        public bool Sabato { get; set; }

        public bool Domenica { get; set; }

        public void ResetGiorni()
        {
            Giorni = new List<GiornoSettimana>();
            Lunedi = false;
            Martedi = false;
            Mercoledi = false;
            Giovedi = false;
            Venerdi = false;
            Sabato = false;
            Domenica = false;
        }

        public void ResetFasce()
        {
            Mattina = false;
            Pomeriggio = false;
        }
    }
}
