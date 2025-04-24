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

        // Copy template files
        string templateDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "templates");
        if (!Directory.Exists(templateDir))
        {
            Console.WriteLine($"Template directory not found: {templateDir}");
            templateDir = Path.Combine(AppContext.BaseDirectory, "templates");
            if (!Directory.Exists(templateDir))
            {
                Console.WriteLine($"Alternate template directory not found: {templateDir}");
                return;
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
            string rowClass = sesija.Lokacija.Contains("neradni dan") ? " class=\"non-working-day\"" : "";
            sb.AppendLine($"<tr{rowClass}>");
            sb.AppendLine($"<td>{sesija.Dan}</td>");
            sb.AppendLine($"<td>{sesija.Datum}</td>");
            sb.AppendLine($"<td>{sesija.Vrijeme}</td>");
            sb.AppendLine($"<td>{sesija.Lokacija}</td>");
            sb.AppendLine("</tr>");
        }

        return sb.ToString();
    }

    private static string GenerateJsonData(List<PodaciZaSesiju> sesije)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(sesije, options);
    }
}