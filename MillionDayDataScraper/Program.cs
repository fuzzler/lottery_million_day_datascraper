using HtmlAgilityPack;
using System;
using System.Globalization;

namespace MillionDayDataScraper
{
    internal class MDData
    {
        public DateTime Data { get; set; }
        public int Concorso { get; set; }
        public int[] NumeriEstratti { get; set; } = new int[5];
        public int[] NumeriExtra { get; set; } = new int[5];
        public long Timestamp { get; set; }
        //public string OraEstrazione { get; set; }

        public MDData() { }

        public MDData(DateTime data, int concorso, int[] numeriEstratti, int[] numeriExtra, long timestamp = 0)
        {
            Data = data;
            Concorso = concorso;
            NumeriEstratti = numeriEstratti;
            NumeriExtra = numeriExtra;
            //OraEstrazione = ora;
            Timestamp = timestamp;
        }
    }

    internal class Program
    {
        static string BASE_URL = "https://www.archiviomillionday.it/archivio-million-day.php?anno="; // url in cui trovare 
        static string CSV_PATH = "dati_md.csv";    // contiene il percorso del file csv
        static int[] ANNI = { 2024, 2023, 2022, 2021, 2020, 2019, 2018 };
        static List<MDData> ListaDatiEstrazioni = new List<MDData>(); // lista di oggetti che conterranno i dati

        static async Task Main(string[] args)
        {
            // Ciclo per i vari anni dei concorsi MD
            foreach (int anno in ANNI)
            {
                string url = BASE_URL + anno;

                // Preleva contenuto della pagina
                string htmlContent = await GetPageContent(url);

                ExtractDataFromPageContent(htmlContent);
            }
            
            // Todo:
            // - inverti la lista (per data crescente => dal primo all'ultimo cosi si possono aggiungere in coda nuovi record)
            // - stampa la lista in un file csv
            // - sposta la logica di estrazione di tutti i dati in un solo metodo (commentabile)
            // - creare nuovo metodo che preleva solo le estrazioni del giorno prima (per aggiurnare il file csv)
            // - permettere scelta operazione all'inizio (prelievo tutti i dati / aggiornamento csv / ... )
            
            // Stampa finale
            StampaListaEstrazioni(ListaDatiEstrazioni);
        }

        static void ExtractDataFromPageContent(string htmlContent)
        {
            // Analizza il contenuto HTML
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Trova la tabella contenente i dati
            var table = htmlDoc.DocumentNode.SelectSingleNode("//table[@class='tba']");
            if (table == null)
            {
                throw new Exception("Tabella non trovata nella pagina.");
            }

            // Estrai le righe dalla tabella
            var rows = table.SelectNodes(".//tr");

            foreach (var row in rows.Skip(1)) // Salta la prima riga (intestazione)
            {
                var cells = row.SelectNodes(".//td");
                if (cells != null && cells.Count >= 2) // Assicura almeno due colonne
                {
                    // Assegnazione data
                    DateTime data = new DateTime();

                    string dtr = cells[0].InnerText.Trim();
                    if (dtr.ToLower().Equals("oggi"))
                        data = DateTime.Today;
                    else if (dtr.ToLower().Equals("ieri"))
                        data = DateTime.Today.AddDays(-1);
                    else
                        data = DateTime.Parse(dtr);

                    int.TryParse(cells[1].InnerText.Trim(), out int concorso);
                    string[] estratti = cells[2].InnerText.Trim().Split(" ");
                    string[] extra = cells[3].InnerText.Trim().Split(" ");
                    TimeSpan ora = new TimeSpan(13, 0, 0); // Nuova ora (15:30:00)


                    if (concorso % 2 == 0)
                        ora = new TimeSpan(20, 30, 0);

                    data = data.Date + ora;

                    // Data per conversione in timestamp
                    string dataStr = data.ToString("dd-MM-yyyy HH:mm");

                    ListaDatiEstrazioni.Add(new MDData(data, concorso,
                        ConvertStrArrayToIntArray(estratti), ConvertStrArrayToIntArray(extra),
                        ConvertToTimestamp(dataStr)));
                }
            }
        }

        async static Task<string> GetPageContent(string url)
        {
            // Scarica il contenuto HTML della pagina
            HttpClient client = new HttpClient();
            string htmlContent = await client.GetStringAsync(url);

            return htmlContent;
        }

        static int[] ConvertStrArrayToIntArray(string[] strArray)
        {
            int[] intArray = new int[strArray.Length];
            int i = 0;

            foreach (string nr in strArray)
            {
                if(int.TryParse(nr, out int n))
                    intArray[i++] = n;
                else
                    throw new Exception("Impossibile convertire valore: " + nr);
            }

            return intArray;
        }

        static long ConvertToTimestamp(string dateString)
        {
            // Formato della data di input
            string format = "dd-MM-yyyy HH:mm";
            long timestamp = 0;

            // Parsing della stringa in un oggetto DateTime
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                timestamp = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
                // DEBUG: Console.WriteLine($"La data {dateString} corrisponde al timestamp Unix: {timestamp}");
            }
            else
            {
                throw new Exception("Formato della data non valido.");
            }

            if(timestamp == 0)
                throw new Exception("Errore conversione timestamp.");

            return timestamp;
        }

        static void StampaListaEstrazioni(List<MDData> lista)
        {
            Console.WriteLine("Timestamp\tData e Ora\tConcorso\tNumeri Estratti\tNumeri Extra"); // intestazione

            foreach (MDData md in lista)
            {
                Console.WriteLine($"{md.Timestamp}\t{md.Data}\t{md.Concorso}" + // \t{md.OraEstrazione}
                    $"\t{string.Join(" ", md.NumeriEstratti)}" +
                    $"\t{string.Join(" ", md.NumeriExtra)}");
            }
        }
    }
}
