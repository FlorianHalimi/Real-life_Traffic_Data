using CsvHelper;
using Newtonsoft.Json.Linq;
using System.Globalization;
class Program
{
    static readonly HttpClient client = new HttpClient();
    static async Task Main(string[] args)
    {
        string inputFolderPath = "../../../DataInput/";
        string outputFolderPath = "../../../DataOutput/";

        Directory.CreateDirectory(outputFolderPath);

        string[] csvFiles = Directory.GetFiles(inputFolderPath, "*.csv");

        foreach (string filePath in csvFiles)
        {
            Console.WriteLine($"Processing file: {Path.GetFileName(filePath)}");

            List<dynamic> csvData = await ReadCsvFile(filePath);

            if (csvData != null)
            {
                int totalRows = csvData.Count;
                int currentRow = 0;

                bool passengerPickedUp = false;
                List<string> streetNames = new List<string>();
                List<string> routesToWrite = new List<string>();

                foreach (dynamic row in csvData)
                {
                    currentRow++;

                    double progressPercentage = (double)currentRow / totalRows * 100;

                    Console.WriteLine($"Progress: {progressPercentage:F2}% Rows Read: {currentRow}/{totalRows}");

                    int di2 = int.Parse(row.Di2);

                    if (di2 == 1)
                    {
                        passengerPickedUp = true;
                        double latitude = double.Parse(row.Latitude);
                        double longitude = double.Parse(row.Longitute);
                        string streetName = await GetStreetName(latitude, longitude);
                        if (streetName == "")
                        {
                            continue;
                        }
                        streetName = streetName.Replace(" ", "_");
                        if (!streetNames.Any(x => x == streetName))
                        {
                            streetNames.Add(streetName);
                        }
                    }
                    else if (di2 == 0 && passengerPickedUp)
                    {
                        passengerPickedUp = false;
                        if (streetNames.Any())
                        {
                            int length = streetNames.Count;
                            routesToWrite.Add($"{length}");
                            routesToWrite.AddRange(streetNames);
                            routesToWrite.Add("\n");
                            streetNames.Clear();
                        }
                    }

                    await Task.Delay(100); 
                }

                string outputFileName = Path.GetFileNameWithoutExtension(filePath) + "_PassengerRoutes.txt";
                string outputPath = Path.Combine(outputFolderPath, outputFileName);

                WriteRoutesToFile(routesToWrite, outputPath);
                Console.WriteLine($"Results written to {outputFileName}");
            }
            else
            {
                Console.WriteLine($"Error reading CSV file: {filePath}");
            }
        }
    }

    static void WriteRoutesToFile(List<string> routes, string outputPath)
    {
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            foreach (string route in routes)
            {
                writer.Write(route + " "); 
            }
            writer.WriteLine();
        }
    }

    static async Task<List<dynamic>> ReadCsvFile(string filePath)
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<dynamic>().ToList();
            return await Task.FromResult(records);
        }
    }

    static async Task<string> GetStreetName(double latitude, double longitude)
    {
        string apiUrl = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=18&addressdetails=1";
        client.DefaultRequestHeaders.UserAgent.ParseAdd("YourUserAgent");
        HttpResponseMessage response = await client.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseBody);
            JToken roadToken = json.SelectToken("address.road");
            if (roadToken != null)
            {
                return roadToken.ToString();
            }
            else
            {
                return "";
            }
        }
        else
        {
            return "Error fetching street name";
        }
    }
}