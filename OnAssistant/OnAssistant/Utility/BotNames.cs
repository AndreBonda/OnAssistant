using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnAssistant.Utility
{
    /// <summary>
    /// Nomi delle reference relative alle applicazioni LUIS. Coincidono con i nomi riportati sul portale luis.ai
    /// </summary>
    public class BotNames
    {
        // Moduli LUIS
        public const string LuisDispatch = "OnAssistantDispatch";
        public const string LuisPrenotazione = "OnAssistant-prenotazione";
        public const string LuisRefertazione = "OnAssistant-refertazione";

        // Intent LUIS
        public const string IntentOrario = "Inserimento_filtri_giorni_fascia";
        public const string IntentAvanti = "Avanti";
        public const string IntentIndietro = "Indietro";
        public const string IntentConferma = "Confermare";
        public const string IntentNega = "Negare";
        public const string IntentStop = "Stop";

        // ID dialogs
        public const string Root = "root";
        public const string TextCF = "text_cf";
        public const string TextRE = "text_re";
        public const string Text = "text";
        public const string NewAppDialog = "nuovo_app_dialogo";
        public const string ModifyAppDIalog = "modifica_app_dialogo";
        public const string CancelAppDialog = "cancella_app_dialogo";
        public const string AppActionNotDetected = "app_action_not_detected";
        public const string CodiceFiscale = "codiceFiscale";
        public const string CodiceRE = "codiceRE";
        public const string Orario = "orario";
        public const string CercaDisponibilita = "cerca_disponibilita";

        // Entità LUIS
        public const string EntityNuovoApp = "nuovo";
        public const string EntitySpostaApp = "modifica";
        public const string EntityCancellaApp = "cancella";
        public static readonly string[] PrenotationEntities = { "Operation", "Giorno", "aGiorno", "daGiorno", "fasciaOra" };
        public static readonly string[] FasciaOrariaEntities = { "Giorno", "aGiorno", "daGiorno", "fasciaOra" };
        public static readonly string EntityPrenotazione = "Operation";

        /// <summary>
        /// Enumerazione giorni settimana.
        /// </summary>
        public enum GiornoSettimana
        {
            Lunedi = 0,
            Martedi = 1,
            Mercoledi = 2,
            Giovedi = 3,
            Venerdi = 4,
            Sabato = 5,
            Domenica = 6
        }

        /// <summary>
        /// Enumerazione fasce giornaliere.
        /// </summary>
        public enum FasciaGiorno
        {
            mattina,
            pomeriggio
        }

        public class FiltriGiorniFascia
        {
            public bool Lunedi { get; set; }

            public bool Martedi { get; set; }

            public bool Mercoledi { get; set; }

            public bool Giovedi { get; set; }

            public bool Venerdi { get; set; }

            public bool Sabato { get; set; }

            public bool Domenica { get; set; }

            public bool Mattina { get; set; }

            public bool Pomeriggio { get; set; }
        }
    }
}
