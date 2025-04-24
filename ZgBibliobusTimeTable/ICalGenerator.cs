using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace ZgBibliobusTimeTable;

/// <summary>
/// Generates iCalendar (.ics) files for schedule data
/// </summary>
public static class ICalGenerator
{
    /// <summary>
    /// Generates iCalendar files (all locations and per-location) from bibliobus schedule data
    /// </summary>
    /// <param name="sesije">The list of schedule sessions</param>
    /// <param name="outputDir">The directory where to save the iCalendar files</param>
    public static void GenerateICalendars(List<PodaciZaSesiju> sesije, string outputDir)
    {
        // Get all unique locations (excluding non-working days)
        var allLocations = sesije
            .Where(s => !s.Lokacija.Contains("neradni dan") && !string.IsNullOrEmpty(s.Vrijeme))
            .Select(s => s.Lokacija)
            .Distinct()
            .OrderBy(loc => loc)
            .ToList();
            
        // Generate the main calendar with all locations
        string mainCalendarPath = Path.Combine(outputDir, "bibliobus-calendar.ics");
        GenerateICalendar(sesije, mainCalendarPath);
        
        // Generate individual calendars for each location
        foreach (var location in allLocations)
        {
            // Create a sanitized filename from the location
            string safeLocation = SanitizeFilename(location);
            string locationCalendarPath = Path.Combine(outputDir, $"bibliobus-{safeLocation}.ics");
            
            // Filter sessions for this location only
            var locationSessions = sesije
                .Where(s => s.Lokacija.Equals(location) || s.Lokacija.Contains("neradni dan"))
                .ToList();
                
            GenerateICalendar(locationSessions, locationCalendarPath, location);
            
            Console.WriteLine($"Location calendar generated for '{location}' at {locationCalendarPath}");
        }
        
        // Generate a JSON file containing location information for the UI
        GenerateLocationInfo(allLocations, outputDir);
    }
    
    private static void GenerateLocationInfo(List<string> locations, string outputDir)
    {
        var locationInfo = locations.Select(loc => new
        {
            name = loc,
            value = SanitizeFilename(loc),
            calendarFile = $"bibliobus-{SanitizeFilename(loc)}.ics"
        });
        
        string json = System.Text.Json.JsonSerializer.Serialize(locationInfo, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        
        string locationInfoPath = Path.Combine(outputDir, "bibliobus-locations.json");
        File.WriteAllText(locationInfoPath, json);
        Console.WriteLine($"Location info generated at {locationInfoPath}");
    }
    
    /// <summary>
    /// Helper method to sanitize a location name for use in filenames
    /// </summary>
    private static string SanitizeFilename(string input)
    {
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

    /// <summary>
    /// Generates an iCalendar file from bibliobus schedule data
    /// </summary>
    /// <param name="sesije">The list of schedule sessions</param>
    /// <param name="filePath">The path where to save the iCalendar file</param>
    /// <param name="locationName">Optional location name for single-location calendars</param>
    public static void GenerateICalendar(List<PodaciZaSesiju> sesije, string filePath, string locationName = null)
    {
        var sb = new StringBuilder();

        // Begin iCalendar
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//ZgBibliobusTimeTable//Zagreb Bibliobus Calendar//HR");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        
        // Set calendar name based on whether it's for a specific location
        if (string.IsNullOrEmpty(locationName))
        {
            sb.AppendLine($"X-WR-CALNAME:Zagreb Bibliobus");
            sb.AppendLine("X-WR-CALDESC:Raspored zagrebačkog bibliobusa");
        }
        else
        {
            sb.AppendLine($"X-WR-CALNAME:Zagreb Bibliobus - {locationName}");
            sb.AppendLine($"X-WR-CALDESC:Raspored zagrebačkog bibliobusa za lokaciju {locationName}");
        }
        
        sb.AppendLine("X-WR-TIMEZONE:Europe/Zagreb");
        
        // Add all events
        int eventCount = 0;
        DateTime now = DateTime.UtcNow;
        string nowString = now.ToString("yyyyMMddTHHmmssZ");
        
        foreach (var sesija in sesije)
        {
            // Skip sessions without time information (like non-working days)
            if (string.IsNullOrEmpty(sesija.Vrijeme) || sesija.Lokacija.Contains("neradni dan"))
            {
                // For non-working days, create a special all-day event
                if (sesija.Lokacija.Contains("neradni dan"))
                {
                    eventCount++;
                    string uid = $"zgbibliobus-{eventCount}@kgz.hr";
                    
                    // Parse the date
                    if (DateTime.TryParse(sesija.Datum, out DateTime nonWorkingDate))
                    {
                        string dateString = nonWorkingDate.ToString("yyyyMMdd");
                        
                        sb.AppendLine("BEGIN:VEVENT");
                        sb.AppendLine($"UID:{uid}");
                        sb.AppendLine($"DTSTAMP:{nowString}");
                        sb.AppendLine($"DTSTART;VALUE=DATE:{dateString}");
                        sb.AppendLine($"DTEND;VALUE=DATE:{dateString}");
                        sb.AppendLine($"SUMMARY:Bibliobus - Neradni dan");
                        sb.AppendLine("TRANSP:TRANSPARENT");
                        sb.AppendLine("END:VEVENT");
                    }
                }
                continue;
            }

            // Parse the date and time information
            if (DateTime.TryParse(sesija.Datum, out DateTime eventDate))
            {
                // Parse time range (format: "09:00-11:15")
                string[] timeRange = sesija.Vrijeme.Split('-');
                if (timeRange.Length != 2)
                {
                    continue; // Skip if time format is not as expected
                }

                // Parse start time
                string[] startTimeParts = timeRange[0].Trim().Split(':');
                if (startTimeParts.Length != 2 || 
                    !int.TryParse(startTimeParts[0], out int startHour) || 
                    !int.TryParse(startTimeParts[1], out int startMinute))
                {
                    continue; // Skip if start time cannot be parsed
                }

                // Parse end time
                string[] endTimeParts = timeRange[1].Trim().Split(':');
                if (endTimeParts.Length != 2 || 
                    !int.TryParse(endTimeParts[0], out int endHour) || 
                    !int.TryParse(endTimeParts[1], out int endMinute))
                {
                    continue; // Skip if end time cannot be parsed
                }

                // Create start and end DateTime objects
                DateTime startDateTime = new DateTime(
                    eventDate.Year, eventDate.Month, eventDate.Day, 
                    startHour, startMinute, 0);
                
                DateTime endDateTime = new DateTime(
                    eventDate.Year, eventDate.Month, eventDate.Day, 
                    endHour, endMinute, 0);

                // Format dates for iCalendar
                string startDateTimeString = startDateTime.ToString("yyyyMMddTHHmmss");
                string endDateTimeString = endDateTime.ToString("yyyyMMddTHHmmss");

                // Create a unique ID for this event
                eventCount++;
                string uid = $"zgbibliobus-{eventCount}@kgz.hr";

                // Add the event
                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"UID:{uid}");
                sb.AppendLine($"DTSTAMP:{nowString}");
                sb.AppendLine($"DTSTART:{startDateTimeString}");
                sb.AppendLine($"DTEND:{endDateTimeString}");
                sb.AppendLine($"SUMMARY:Bibliobus - {sesija.Lokacija}");
                sb.AppendLine($"LOCATION:{sesija.Lokacija}");
                sb.AppendLine($"DESCRIPTION:Bibliobus u {sesija.Lokacija} ({sesija.Dan})");
                
                // Add reminder alert 30 minutes before
                sb.AppendLine("BEGIN:VALARM");
                sb.AppendLine("ACTION:DISPLAY");
                sb.AppendLine("DESCRIPTION:Reminder");
                sb.AppendLine("TRIGGER:-PT30M");
                sb.AppendLine("END:VALARM");
                
                sb.AppendLine("END:VEVENT");
            }
        }

        // End iCalendar
        sb.AppendLine("END:VCALENDAR");

        // Write to file
        File.WriteAllText(filePath, sb.ToString());

        if (string.IsNullOrEmpty(locationName))
        {
            Console.WriteLine($"iCalendar file generated at {filePath} with {eventCount} events.");
        }
    }
}