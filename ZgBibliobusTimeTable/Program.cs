using System.IO;
using System.Text.Json;

namespace ZgBibliobusTimeTable;

internal class Program : Tools
{
    static void Main(string[] args)
    {
        //>>> uncomment >>>  the GetWebContent line to fetch the content from the URL
        //>>> uncomment >>>  and save it to the specified file

        // Use a direct path in the application directory
        string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        
        // Create the directory if it doesn't exist
        if (!Directory.Exists(dataFolder))
            Directory.CreateDirectory(dataFolder);
            
        string htmlFilename = Path.Combine(dataFolder, "index.html");

        // get the page content from the url and save it to the file
        //>>> uncomment >>>  string url = "https://www.kgz.hr/hr/knjiznice/bibliobusna-sluzba/raspored-bibliobusnih-stajalista/65960";
        //>>> uncomment >>>  WebContent.GetWebContent(url, htmlFilename);  //Process.Start("NOTEPAD.EXE", htmlFilename);

        string pageContent = System.IO.File.ReadAllText(htmlFilename);

        List<PodaciZaDan> dani = WebContent.ParseWebContent(pageContent);

        List<PodaciZaSesiju> sesije = Tools.PretvoriUSesije(dani);

        // Print schedule to console
        foreach (var sesija in sesije)
        {
            Console.WriteLine(sesija);
        }

        // Generate website files
        string websiteDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "website");
        WebsiteGenerator.GenerateWebsite(sesije, websiteDir);
        
        // Generate iCalendar file
        string icalPath = Path.Combine(websiteDir, "bibliobus-calendar.ics");
        ICalGenerator.GenerateICalendar(sesije, icalPath);

        Console.WriteLine("GOTOVO!");
    }
}
