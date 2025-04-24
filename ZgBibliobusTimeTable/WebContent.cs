using HtmlAgilityPack;
using System.Text;

namespace ZgBibliobusTimeTable;

public static class WebContent
{
    public static List<PodaciZaDan> ParseWebContent(string pageContent)
    {
        List<PodaciZaDan> dani = new();
        PodaciZaDan tekuciDan = new();

        // Parse the HTML content using HtmlAgilityPack
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(pageContent);

        // Extract specific elements (e.g., all <a> tags)
        var tables = doc.DocumentNode.SelectNodes("//table");
        if (tables is null)
            throw new Exception("Nema <TABLE> elemenata.");

        foreach (var table in tables)
        {
            var rows = table.SelectNodes(".//tr");
            if (rows is null)
                throw new Exception("Nema <TR> elemenata.");

            foreach (var row in rows)
            {
                var tableDatas = row.SelectNodes(".//td");
                if (tableDatas is null)
                    throw new Exception("Nema <TR> elemenata.");

                int tdCount = tableDatas.Count();

                if (tdCount == 3)
                {
                    tekuciDan = new PodaciZaDan();
                    tekuciDan.danNode = tableDatas[0];
                    // Store InnerHtml instead of InnerText to preserve links
                    tekuciDan.VremenaILokacije.Add(tableDatas[2].InnerHtml + "#" + tableDatas[1].InnerHtml);
                    dani.Add(tekuciDan);
                }
                else if (tdCount == 2)
                {
                    if (tekuciDan.danNode is null)
                        throw new Exception("Tekuci dan je prazan.");

                    // Store InnerHtml instead of InnerText to preserve links
                    tekuciDan.VremenaILokacije.Add(tableDatas[1].InnerHtml + "#" + tableDatas[0].InnerHtml);
                }
                else
                    throw new Exception("TableDatas count is not 2 or 3.");
            }
            tekuciDan = new();
        }

        return dani;
    }

    public static void GetWebContent(string url, string htmlFilename)
    {
        Task.Run(() => GetWebContentAsync(url, htmlFilename)).GetAwaiter().GetResult();
    }

    public static async Task GetWebContentAsync(string url, string htmlFilename)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        //Encoding win1250 = Encoding.GetEncoding(1250);
        UTF8Encoding utf8Encoding = new UTF8Encoding(false, true); // BOM = false, EncoderShouldEmitUTF8Identifier = true

        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Fetch the HTML content of the web page
                var response = await client.GetByteArrayAsync(url);

                Console.WriteLine("Page content fetched successfully.");

                //string s = win1250.GetString(response);
                string s = utf8Encoding.GetString(response);

                System.IO.File.WriteAllText(htmlFilename, s);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
