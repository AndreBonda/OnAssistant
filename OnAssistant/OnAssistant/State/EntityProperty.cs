using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnAssistant.State
{
    /// <summary>
    /// Questa classe rappresenta le entità restituite dal servizio NLP LUIS.
    /// </summary>
    public class EntityProperty
    {
        public EntityProperty(string name, List<string> value)
        {
            EntityName = name;
            Value = value;
        }

        /// <summary>
        /// Gets or sets il nome dell'entità.
        /// </summary>
        /// <value>
        /// Rappresenta il nome dell'entità.
        /// </value>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets i valori dell'entità.
        /// </summary>
        /// <value>
        /// Rappresenta il valore dell'entità.
        /// </value>
        public List<string> Value { get; set; }
    }
}
