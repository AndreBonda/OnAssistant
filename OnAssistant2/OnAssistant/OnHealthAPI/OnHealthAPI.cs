using OnAssistant.State;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static OnAssistant.OnHealthAPI.OnHealthAPI.RicercaDisponibilitaDTO;

namespace OnAssistant.OnHealthAPI
{
    public class OnHealthAPI
    {

        /// <summary>
        /// Chiamta HTTP che recupera il paziente web su OnHealth associato al codice fiscale.
        /// </summary>
        /// <param name="cf"> Codice fiscale utente.</param>
        /// <returns>
        /// Ritorna l'id del paziente web.
        /// </returns>
        public static async Task<string> RecuperaCodicePazienteWeb(string cf)
        {
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString["IdAzienda"] = "080112";
            queryString["ManagerAppId"] = "OnHospital";
            queryString["Cf"] = cf;
            PazienteWebDTO result = new PazienteWebDTO();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("OnHospitalAPI_APK", "F70AA0F0-9CEF-4A82-93C0-D4FAA65DA02D");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_ManagerAppId", "OnHospital");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_IdAzienda", "080112");

                string url = string.Format("http://localhost/On.Portal/On.Health/On.Hospital/API/PrenotazioneIntegrata/RecuperaIdPazienteWeb?{0}", queryString.ToString());
                using (var stream = await client.GetStreamAsync(url))
                using (var lettura = new StreamReader(stream))
                {
                    var jsonString = await lettura.ReadToEndAsync();
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<PazienteWebDTO>(jsonString);
                }
            }

            return result.IdPazienteWeb;
        }

        /// <summary>
        /// Chiamata HTTP che recupera le prestazioni associate alla dematerializzata.
        /// </summary>
        /// <param name="nre">Codice ricetta elettronica.</param>
        /// <param name="pawCodice">Id del paziente web.</param>
        /// <returns>
        /// L'elenco delle prestazioni associate alla ricetta elettronica.
        /// </returns>
        public static async Task<PrestazioniDTO> RicercaPrestazioniDema(string nre, string pawCodice)
        {
            PrestazioniDTO result = new PrestazioniDTO();
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString["IdAzienda"] = "080112";
            queryString["ManagerAppId"] = "OnHospital";
            queryString["CodiceNRE"] = nre;
            queryString["IdPaziente"] = pawCodice;
            queryString["TipoPrenWeb"] = "0";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("OnHospitalAPI_APK", "F70AA0F0-9CEF-4A82-93C0-D4FAA65DA02D");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_ManagerAppId", "OnHospital");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_IdAzienda", "080112");

                string url = string.Format("http://localhost/On.Portal/On.Health/On.Hospital/API/PrenotazioneIntegrata/RicercaPrestazioniDema?{0}", queryString.ToString());
                using (var stream = await client.GetStreamAsync(url))
                using (var lettura = new StreamReader(stream))
                {
                    var jsonString = await lettura.ReadToEndAsync();
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<PrestazioniDTO>(jsonString);
                }
            }

            return result;
        }

        /// <summary>
        /// Chiamata HTTP che recupera le disponibilità per una prestazione.
        /// </summary>
        /// <param name="nre">Codice ricetta dematerializzata.</param>
        /// <param name="pawCodice">Id del paziente web.</param>
        /// <param name="idPrestazione">Id prestazione.</param>
        /// <param name="filtri">Filtri di ricerca per giorni settimanali e fascia giornaliera.</param>
        /// <returns>
        /// Elenco delle disponibilità.
        /// </returns>
        public static async Task<RicercaDisponibilitaDTO> RicercaDisponibilita(string nre, string pawCodice, string idPrestazione, FiltriGiorniFascia filtri)
        {
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString["IdAzienda"] = "080112";
            queryString["ManagerAppId"] = "OnHospital";
            queryString["IdPaziente"] = pawCodice;
            queryString["CodiceNRE"] = nre;
            queryString["T"] = TipiDiPrenotazione.D.ToString();
            queryString["TipoPrenWeb"] = TipologiaPrenotazioneWeb.Cittadino.ToString();
            queryString["IdPrestazioniCSV"] = idPrestazione;

            // da domani per una settimana
            queryString["IData"] = DateTime.Today.AddDays(1).ToString("O");
            queryString["FData"] = DateTime.Today.AddDays(8).ToString("O");
            queryString["lun"] = filtri.Lunedi.ToString();
            queryString["mar"] = filtri.Martedi.ToString();
            queryString["mer"] = filtri.Mercoledi.ToString();
            queryString["gio"] = filtri.Giovedi.ToString();
            queryString["ven"] = filtri.Venerdi.ToString();
            queryString["sab"] = filtri.Sabato.ToString();
            queryString["dom"] = filtri.Domenica.ToString();

            if (filtri.Mattina && filtri.Pomeriggio)
            {
                queryString["IOra"] = new TimeSpan(0, 0, 0).ToString(@"hh\:mm\:ss");
                queryString["FOra"] = new TimeSpan(23, 59, 59).ToString(@"hh\:mm\:ss");
            }
            else if (filtri.Mattina)
            {
                queryString["IOra"] = new TimeSpan(0, 0, 0).ToString(@"hh\:mm\:ss");
                queryString["FOra"] = new TimeSpan(13, 59, 59).ToString(@"hh\:mm\:ss");
            }
            else if (filtri.Pomeriggio)
            {
                queryString["IOra"] = new TimeSpan(14, 0, 0).ToString(@"hh\:mm\:ss");
                queryString["FOra"] = new TimeSpan(23, 59, 59).ToString(@"hh\:mm\:ss");
            }

            RicercaDisponibilitaDTO result = new RicercaDisponibilitaDTO();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("OnHospitalAPI_APK", "F70AA0F0-9CEF-4A82-93C0-D4FAA65DA02D");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_ManagerAppId", "OnHospital");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_IdAzienda", "080112");
                string url = string.Format("http://localhost/On.Portal/On.Health/On.Hospital/API/PrenotazioneIntegrata/RicercaDisponibilita?{0}", queryString.ToString());
                using (var stream = await client.GetStreamAsync(url))
                using (var lettura = new StreamReader(stream))
                {
                    var jsonString = await lettura.ReadToEndAsync();
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RicercaDisponibilitaDTO>(jsonString);
                }
            }

            return result;
        }

        /// <summary>
        /// Chiamata HTTP che recupera una disponibilità specifica.
        /// </summary>
        /// <param name="nre">Codice ricetta dematerializzata.</param>
        /// <param name="pawCodice">Id del paziente web.</param>
        /// <param name="idPrestazione">Id prestazione.</param>
        /// <param name="proposta">Proposta da bloccare.</param>
        /// <param name="contatoreOrario">Contatore dell'orario da bloccare all'interno di una proposta.</param>
        /// <returns>
        /// Disponibilità richiesta ancora presente.
        /// </returns>
        public static async Task<RicercaDisponibilitaDTO> SelezionaDisponibilita(string nre, string pawCodice, string idPrestazione, PropostaAppuntamento_DTO proposta, int contatoreOrario)
        {
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString["IdAzienda"] = "080112";
            queryString["ManagerAppId"] = "OnHospital";
            queryString["IdPaziente"] = pawCodice;
            queryString["CodiceNRE"] = nre;
            queryString["T"] = TipiDiPrenotazione.D.ToString();
            queryString["TipoPrenWeb"] = TipologiaPrenotazioneWeb.Cittadino.ToString();
            queryString["IdPrestazioniCSV"] = idPrestazione;
            queryString["IdUni"] = proposta.IdUnitaErogatrice;
            queryString["IdSede"] = proposta.IdSede;

            // inizio data e fine data sono quelle della proposta da bloccare
            queryString["IData"] = proposta.Data.Value.ToString("O");
            queryString["FData"] = proposta.Data.Value.ToString("O");

            // Inizio ora deve essere l'orario della disponibilità da bloccare, il fine ora aggiunta di 10 minuti
            queryString["IOra"] = proposta.Orari.ElementAt(contatoreOrario).ToShortTimeString();
            queryString["FOra"] = proposta.Orari.ElementAt(contatoreOrario).AddMinutes(10).ToShortTimeString();

            RicercaDisponibilitaDTO result = new RicercaDisponibilitaDTO();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("OnHospitalAPI_APK", "F70AA0F0-9CEF-4A82-93C0-D4FAA65DA02D");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_ManagerAppId", "OnHospital");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_IdAzienda", "080112");
                string url = string.Format("http://localhost/On.Portal/On.Health/On.Hospital/API/PrenotazioneIntegrata/RicercaDisponibilita?{0}", queryString.ToString());
                using (var stream = await client.GetStreamAsync(url))
                using (var lettura = new StreamReader(stream))
                {
                    var jsonString = await lettura.ReadToEndAsync();
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RicercaDisponibilitaDTO>(jsonString);
                }
            }

            return result;
        }

        /// <summary>
        /// Chiamata HTTP che richiede il blocco di una disponibilità.
        /// </summary>
        /// <param name="nre" > Codice ricetta dematerializzata.</param>
        /// <param name="pawCodice">Id del paziente web.</param>
        /// <param name="idPrestazione">Id prestazione.</param>
        /// <param name="proposta">Proposta da bloccare.</param>
        /// <param name="contatoreOrario">Contatore dell'orario da bloccare all'interno di una proposta.</param>
        /// <returns>
        /// Ritorna DTO contenente un idBlocco della disponibilità bloccata.
        /// </returns>
        public static async Task<BloccaDisponibilitaDTO> BloccaDisponibilita(string nre, string pawCodice, string idPrestazione, PropostaAppuntamento_DTO proposta, int contatoreOrario)
        {
            BloccaDisponibilitaDTO result = new BloccaDisponibilitaDTO();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("OnHospitalAPI_APK", "F70AA0F0-9CEF-4A82-93C0-D4FAA65DA02D");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_ManagerAppId", "OnHospital");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_IdAzienda", "080112");

                DateTime dataOra = proposta.Data.Value.Date + proposta.Orari.ElementAt(contatoreOrario).TimeOfDay;

                var values = new Dictionary<string, string>
                {
                    { "IdAzienda", "080112" },
                    { "IdAziendaEffettiva", "080112" },
                    { "ManagerAppId", "OnHospital" },
                    { "IdPaziente", pawCodice },
                    { "IdPazienteEffettivo", pawCodice },
                    { "IdPrestazioni", idPrestazione },
                    { "IdPrestazioniEffettive", idPrestazione },
                    { "NRE", nre },
                    { "T", TipiDiPrenotazione.D.ToString() },
                    { "IdUni", proposta.IdUnitaErogatrice },
                    { "IdSede", proposta.IdSede },
                    { "DataOraScelta", dataOra.ToString("O") },
                    { "TipoRichiesta", proposta.TipoRichiesta },
                    { "FData", proposta.Data.Value.AddDays(30).ToString("O") },
                    { "IData", DateTime.Today.ToString("O") },
                };
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("http://localhost/On.Portal/On.Health/On.Hospital/API/PrenotazioneIntegrata/BloccaDisponibilita", content);
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var lettura = new StreamReader(stream))
                {
                    var jsonString = await lettura.ReadToEndAsync();
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<BloccaDisponibilitaDTO>(jsonString);
                }
            }

            return result;
        }

        /// <summary>
        /// Chiamata HTTP per effettuare la prenotazione dell'appuntamento.
        /// </summary>
        /// <returns>
        /// Ritorna DTO con l'id dell'appuntamento.
        /// </returns>
        public static async Task<AppuntamentoConfermatoDTO> ConfermaAppuntamento(string pawCodice, string idPrestazione, PropostaAppuntamento_DTO proposta, int contatoreOrario, string idBlocco)
        {
            AppuntamentoConfermatoDTO result = new AppuntamentoConfermatoDTO();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("OnHospitalAPI_APK", "F70AA0F0-9CEF-4A82-93C0-D4FAA65DA02D");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_ManagerAppId", "OnHospital");
                client.DefaultRequestHeaders.Add("OnHospitalAPI_IdAzienda", "080112");

                DateTime dataOra = proposta.Data.Value.Date + proposta.Orari.ElementAt(contatoreOrario).TimeOfDay;

                var values = new Dictionary<string, string>
                {
                    { "IdAzienda", "080112" },
                    { "IdAziendaEffettiva", "080112" },
                    { "ManagerAppId", "OnHospital" },
                    { "IdPaziente", pawCodice },
                    { "IdPazienteEffettivo", pawCodice },
                    { "IdPrestazioni", idPrestazione },
                    { "IdPrestazioniEffettive", idPrestazione },
                    { "T", TipiDiPrenotazione.E.ToString() },
                    { "IdUnitaErogatriciCSV", proposta.IdUnitaErogatrice },
                    { "IdSede", proposta.IdSede },
                    { "TipoPazienteConferma", "0" },
                    { "TipoRichiesta", proposta.TipoRichiesta },
                    { "FData", proposta.Data.Value.AddDays(30).ToString("O") },
                    { "IData", DateTime.Today.ToString("O") },
                    { "IdBlocco", idBlocco },
                    { "DataOraScelta", dataOra.ToString("O") },
                    { "IndirizzoIp", "::1" },
                    { "Host_name", "::1" },
                };
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("http://localhost/On.Portal/On.Health/On.Hospital/API/PrenotazioneIntegrata/ConfermaDisponibilitaBloccata", content);
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var lettura = new StreamReader(stream))
                {
                    var jsonString = await lettura.ReadToEndAsync();
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<AppuntamentoConfermatoDTO>(jsonString);
                }
            }

            return result;
        }

        public class BloccaDisponibilitaDTO
        {
            public BloccaDisponibilitaDTO()
            {
                TipoMessaggio = MessageResultType.Success;
            }

            public MessageResultType TipoMessaggio { get; set; }

            public string Messaggio { get; set; }

            public string IdBlocco { get; set; }
        }

        /// <summary>
        /// Dto restituisto dalle API di OnHealth.
        /// </summary>
        public class PrestazioniDTO
        {
            public string IdPrestazioniCSV { get; set; }

            public IDictionary<string, string> PrestazioniEffettive { get; set; }
        }

        /// <summary>
        /// Dto restituisto dalle API di OnHealth.
        /// </summary>
        public class PazienteWebDTO
        {
            public string IdPazienteWeb { get; set; }
        }

        /// <summary>
        /// Dto restituisto dalle API di OnHealth.
        /// </summary>
        public class RicercaDisponibilitaDTO
        {
            public IEnumerable<PropostaAppuntamento_DTO> Proposte { get; set; }

            public MessageResultType TipoMessaggio { get; set; }

            public string Messaggio { get; set; }

            public class PropostaAppuntamento_DTO
            {
                public string IdPaziente { get; set; }

                public string IdPrestazioniCSV { get; set; }

                // Per eventuale sovracup in sovracup
                public string IdAziendaEffettiva { get; set; }

                public string IdPazienteEffettivo { get; set; }

                public List<InfoPrestazione_DTO> PrestazioniEffettive { get; set; }

                public TipiDiPrenotazione TipoDiPrenotazione { get; set; }

                public string IdRaggruppamentoTipoRichiesta { get; set; }

                public string DescrizioneRaggruppamentoTipoRichiesta { get; set; }

                public string IdUnitaErogatrice { get; set; }

                public string UnitaErogatrice { get; set; }

                public string TipoRichiesta { get; set; }

                public string DescrizioneTipoRichiesta { get; set; }

                public string IdSede { get; set; }

                public string Sede { get; set; }

                public string IdBranca { get; set; }

                public string MnemonicoBranca { get; set; }

                public string Prezzo { get; set; }

                public double? PrezzoConvWeb { get; set; }

                public PagamentoOnline TipoPagOnline { get; set; }

                public string NotePrenotazioneWeb { get; set; }

                public string NoteGenerali { get; set; }

                public string NoteOperatore { get; set; }

                public string NotePaziente { get; set; }

                public string NoteAutoriz { get; set; }

                //Nota impostate ed utilizzzate in prenotazione web (da non cancellare)
                public string NoteFisseAllaConfermaPrenotazione { get; set; }

                // Apuntamenti effettivi
                public IEnumerable<DateTime> Orari { get; set; }

                public DateTime? Data { get; set; }

                // Appuntamenti a messaggi
                public string Provincia { get; set; }

                public string Indirizzo { get; set; }

                public string Url_InfoSede { get; set; }

                public bool VisualAvvPrivacy { get; set; }

                public bool RichPrescrizione { get; set; }

                [DebuggerStepThrough]
                public PropostaAppuntamento_DTO()
                {
                    Orari = new List<DateTime>(0);
                    PrestazioniEffettive = new List<InfoPrestazione_DTO>();
                }
            }

        }

        /// <summary>
        /// DTO restituito dalle API di OnHealth.
        /// </summary>
        public partial class AppuntamentoConfermatoDTO
        {
            [DebuggerStepThrough]
            public AppuntamentoConfermatoDTO()
            {
                TipoMessaggio = MessageResultType.Success;
            }

            public MessageResultType TipoMessaggio { get; set; }

            public string Messaggio { get; set; }

            [Obsolete]
            public string Id { get; set; }

            public List<string> IdAppuntamenti { get; set; }
        }

        /// <summary>
        /// Flags e bitwise operators: http://stackoverflow.com/questions/8447/what-does-the-flags-enum-attribute-mean-in-c
        /// </summary>
        [Flags]
        public enum TipiDiPrenotazione
        {
            Nessuna = 0,
            /// <summary>
            /// Prenotazione effettiva (automatica)
            /// </summary>
            E = 1,
            /// <summary>
            /// Prenotazione a messaggi
            /// </summary>
            M = 2,
            /// <summary>
            /// Prenotazione automatica con Ricetta Rossa
            /// </summary>
            R = 4,
            /// <summary>
            /// Prenotazione automatica con Ricetta Dema
            /// </summary>
            D = 8,
        }

        public class InfoDTO
        {
            public string Id { get; set; }

            public string Mnemonico { get; set; }

            public string Descrizione { get; set; }
        }

        public class InfoPrestazione_DTO : InfoDTO
        {
            public string MnemBranca { get; set; }
        }

        public enum MessageResultType
        {
            Success,
            Alert,
            Error
        }

        public enum TipologiaPrenotazioneWeb
        {
            Cittadino = 0,
            Medici = 1
        }

        public enum PagamentoOnline
        {
            Facoltativo = 0,
            Disattivato = 1,
            Obbligatorio = 2,
        }

    }
}