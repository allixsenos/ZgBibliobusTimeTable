using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZgBibliobusTimeTable;

/// <summary>
/// Generates a website with the bibliobus schedule data
/// </summary>
public static class WebsiteGenerator
{
    /// <summary>
    /// Generates the website HTML and JSON files from the schedule data
    /// </summary>
    /// <param name="sesije">The list of schedule sessions</param>
    /// <param name="outputDir">The directory where to output the website files</param>
    public static void GenerateWebsite(List<PodaciZaSesiju> sesije, string outputDir)
    {
        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // Try multiple possible template directories
        string templateDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "templates");
        
        // Add debug info
        Console.WriteLine($"Trying template directory: {templateDir}");
        
        if (!Directory.Exists(templateDir))
        {
            Console.WriteLine($"Template directory not found, trying alternate path");
            templateDir = Path.Combine(AppContext.BaseDirectory, "templates");
            Console.WriteLine($"Trying alternate template directory: {templateDir}");
            
            if (!Directory.Exists(templateDir))
            {
                Console.WriteLine($"Alternate template directory not found, trying repository root");
                // Try at the repository root level
                templateDir = "templates";
                Console.WriteLine($"Trying repository root template directory: {templateDir}");
                
                if (!Directory.Exists(templateDir))
                {
                    Console.WriteLine($"All template directories not found, creating empty templates");
                    Directory.CreateDirectory(templateDir);
                    
                    // Create basic templates
                    File.WriteAllText(Path.Combine(templateDir, "index.html"), 
                        "<!DOCTYPE html><html><head><title>Bibliobus Schedule</title><style>body{font-family:sans-serif;}</style></head>" +
                        "<body><h1>Bibliobus Schedule</h1><div>{{TABLE_DATA}}</div><script>const data={{JSON_DATA}};</script></body></html>");
                    
                    File.WriteAllText(Path.Combine(templateDir, "styles.css"), 
                        "body { font-family: sans-serif; } table { border-collapse: collapse; } " +
                        "th, td { border: 1px solid #ddd; padding: 8px; }");
                }
            }
        }

        // Copy CSS file
        string cssSource = Path.Combine(templateDir, "styles.css");
        string cssTarget = Path.Combine(outputDir, "styles.css");
        if (File.Exists(cssSource))
        {
            File.Copy(cssSource, cssTarget, true);
        }
        else
        {
            Console.WriteLine($"CSS template file not found: {cssSource}");
        }

        // Read HTML template
        string htmlTemplatePath = Path.Combine(templateDir, "index.html");
        if (!File.Exists(htmlTemplatePath))
        {
            Console.WriteLine($"HTML template file not found: {htmlTemplatePath}");
            return;
        }

        string htmlTemplate = File.ReadAllText(htmlTemplatePath);

        // Generate JSON data
        var jsonData = GenerateJsonData(sesije);
        File.WriteAllText(Path.Combine(outputDir, "bibliobus-data.json"), jsonData);

        // Generate table rows
        var tableData = GenerateTableRows(sesije);

        // Replace placeholders in HTML
        string html = htmlTemplate
            .Replace("{{TABLE_DATA}}", tableData)
            .Replace("{{JSON_DATA}}", jsonData)
            .Replace("{{LAST_UPDATED}}", DateTime.Now.ToString("dd.MM.yyyy. HH:mm"));

        // Write HTML file
        File.WriteAllText(Path.Combine(outputDir, "index.html"), html);

        Console.WriteLine($"Website generated in {outputDir}");
    }

    private static string GenerateTableRows(List<PodaciZaSesiju> sesije)
    {
        var sb = new StringBuilder();

        foreach (var sesija in sesije)
        {
            // Convert date from yyyy-MM-dd to dd.MM.yyyy
            string formattedDate = ConvertDateFormat(sesija.Datum);
            
            string rowClass = sesija.Lokacija.Contains("neradni dan") ? " class=\"non-working-day\"" : "";
            sb.AppendLine($"<tr{rowClass}>");
            sb.AppendLine($"<td>{sesija.Dan}</td>");
            sb.AppendLine($"<td>{formattedDate}</td>");
            sb.AppendLine($"<td>{sesija.Vrijeme}</td>");
            sb.AppendLine($"<td>{sesija.Lokacija}</td>");
            sb.AppendLine("</tr>");
        }

        return sb.ToString();
    }
    
    // Helper method to convert date from yyyy-MM-dd to dd.MM.yyyy
    private static string ConvertDateFormat(string isoDate)
    {
        if (DateTime.TryParse(isoDate, out DateTime date))
        {
            return date.ToString("dd.MM.yyyy.");
        }
        return isoDate; // Return original if parsing fails
    }

    private static string GenerateJsonData(List<PodaciZaSesiju> sesije)
    {
        // Get all unique locations (excluding non-working days)
        var allLocations = sesije
            .Where(s => !s.Lokacija.Contains("neradni dan") && !string.IsNullOrEmpty(s.Vrijeme))
            .Select(s => s.Lokacija)
            .Distinct()
            .OrderBy(loc => loc)
            .ToList();
            
        // Create location info for dropdown
        var locationOptions = allLocations.Select(loc => new
        {
            name = loc,
            value = SanitizeLocationId(loc),
            calendarFile = $"bibliobus-{SanitizeLocationId(loc)}.ics"
        }).ToList();
        
        // Add the "All Locations" option
        locationOptions.Insert(0, new 
        { 
            name = "Sve lokacije", 
            value = "", 
            calendarFile = "bibliobus-calendar.ics" 
        });
            
        // Convert sessions to a format that works well with our JavaScript
        var sessionsData = sesije.Select(s => new 
        {
            dan = s.Dan,
            datum = s.Datum,  // Keep ISO format for data consistency
            datumCroatian = ConvertDateFormat(s.Datum),  // Add Croatian format
            vrijeme = s.Vrijeme,
            lokacija = s.Lokacija,
            locationId = SanitizeLocationId(s.Lokacija),
            isNeradniDan = s.Lokacija.Contains("neradni dan")
        });
        
        // Create the combined data object
        var data = new
        {
            locations = locationOptions,
            sessions = sessionsData
        };
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(data, options);
    }
    
    /// <summary>
    /// Helper method to create a valid identifier from a location name
    /// </summary>
    private static string SanitizeLocationId(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Contains("neradni dan"))
            return "";
            
        // Replace spaces, special characters, and diacritics
        string result = input.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace(',', '-')
            .Replace('.', '-')
            .Replace('/', '-')
            .Replace('\\', '-');
            
        // Transliterate Croatian characters
        result = result
            .Replace('č', 'c')
            .Replace('ć', 'c')
            .Replace('đ', 'd')
            .Replace('š', 's')
            .Replace('ž', 'z');
            
        // Remove any other non-alphanumeric characters
        result = new string(result.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        
        // Trim dashes from ends and collapse multiple dashes
        while (result.Contains("--"))
            result = result.Replace("--", "-");
            
        return result.Trim('-');
    }
}