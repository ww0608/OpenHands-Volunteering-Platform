using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenHandsVolunteerPlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private static Dictionary<string, ConversationSession> _sessions = new();
    private readonly ILogger<ChatController> _logger;

    public ChatController(ILogger<ChatController> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("http://localhost:11434/");
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { reply = "Please describe your volunteer opportunity." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { reply = "Please log in to use the chatbot." });
            }

            // Auto-detect new opportunity and clear old session
            bool isNewOpportunity = IsNewOpportunityMessage(request.Message);

            if (_sessions.ContainsKey(userId) && isNewOpportunity)
            {
                var existingData = _sessions[userId].GetCollectedData();
                // If there's existing data and this looks like a new opportunity, clear it
                if (existingData.Count > 0)
                {
                    _logger.LogInformation("Auto-clearing old session for new opportunity");
                    _sessions.Remove(userId);
                }
            }

            // Get existing session or create new one
            if (!_sessions.ContainsKey(userId))
            {
                _sessions[userId] = new ConversationSession(_logger);
            }

            var session = _sessions[userId];
            session.AddMessage(request.Message);

            // Extract data using LLM with regex fallback
            var extracted = await ExtractWithLLM(request.Message);

            // Only update if we extracted something valid
            if (extracted.Count > 0)
            {
                session.UpdateExtractedData(extracted);
            }

            var summary = session.GetSummary();
            var isComplete = session.IsComplete();

            var reply = GenerateTemplateResponse(session.GetCollectedData(), summary, isComplete);

            session.AddAssistantMessage(reply);

            return Ok(new
            {
                reply = reply,
                extracted = session.GetCollectedData(),
                isComplete = isComplete
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat controller");
            return StatusCode(500, new { reply = $"Error: {ex.Message}. Make sure Ollama is running." });
        }
    }

    private bool IsNewOpportunityMessage(string message)
    {
        // Check if message contains patterns of a complete new opportunity
        bool hasTitle = Regex.IsMatch(message, @"^[A-Za-z0-9\s\.]+? need|^[A-Za-z0-9\s\.]+? volunteer", RegexOptions.IgnoreCase);
        bool hasNumber = Regex.IsMatch(message, @"\d+\s*volunteers?", RegexOptions.IgnoreCase);
        bool hasDate = Regex.IsMatch(message, @"\d{1,2}[/-]\d{1,2}[/-]\d{2,4}");
        bool hasTime = Regex.IsMatch(message, @"\d{1,2}\s*(?:am|pm)", RegexOptions.IgnoreCase);
        bool hasLocation = Regex.IsMatch(message, @"\bat\s+[A-Za-z]", RegexOptions.IgnoreCase);

        // If it has at least 3 indicators, it's a new opportunity
        int indicators = (hasTitle ? 1 : 0) + (hasNumber ? 1 : 0) + (hasDate ? 1 : 0) + (hasTime ? 1 : 0) + (hasLocation ? 1 : 0);

        return indicators >= 3;
    }

    [HttpPost("reset")]
    public IActionResult ResetConversation()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId) && _sessions.ContainsKey(userId))
        {
            _sessions.Remove(userId);
        }
        return Ok(new { reply = "Conversation reset. How can I help you create a new opportunity?" });
    }

    [HttpPost("generate-description")]
    public async Task<IActionResult> GenerateDescription([FromBody] DescriptionRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Title))
            {
                return Ok(new { description = GenerateFallbackDescription(request) });
            }

            string prompt = $"Create a short, engaging volunteer description (2-3 sentences) for: {request.Title}. " +
                           $"Need {request.Volunteers} volunteers. " +
                           $"{(request.IsEmergency ? "URGENT: Add crisis/emergency language. " : "")}" +
                           $"Include what volunteers will do (general tasks). " +
                           $"End with 'Sign up now!'. " +
                           $"DO NOT include date, time, or location. " +
                           $"Write only the description, no other text.";

            var response = await CallOllama(prompt);
            string description = response?.Trim() ?? "";
            description = description.Trim('"', '\'');

            if (string.IsNullOrEmpty(description) || description.Length < 30)
            {
                description = GenerateFallbackDescription(request);
            }

            return Ok(new { description = description });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating description");
            return Ok(new { description = GenerateFallbackDescription(request) });
        }
    }

    private string GenerateFallbackDescription(DescriptionRequest request)
    {
        if (request == null)
        {
            return "Join us for this volunteer opportunity! Sign up now and make a difference!";
        }

        if (request.IsEmergency)
        {
            return $"🚨 URGENT: Join us for {request.Title}! We need volunteers immediately. Your help can make a real difference. Sign up now!";
        }

        return $"✨ Join us for {request.Title}! We're looking for enthusiastic volunteers to help out. No experience needed - just bring your energy! Sign up now and be part of something amazing!";
    }

    private async Task<string> CallOllama(string prompt)
    {
        var requestBody = new
        {
            model = "llama3.2:1b",
            prompt = prompt,
            stream = false,
            options = new
            {
                temperature = 0.1,
                top_p = 0.9
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/generate", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

        return result?.response ?? string.Empty;
    }

    private async Task<Dictionary<string, string>> ExtractWithLLM(string message)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();

        try
        {
            var tomorrowDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            var todayDate = DateTime.Now.ToString("yyyy-MM-dd");

            // SIMPLER PROMPT
            var prompt = $@"Extract from this message. Return ONLY JSON.

                Message: {message}

                Extract these fields if present:
                - title: the event name
                - volunteers: the number of volunteers needed
                - date: the date (convert to YYYY-MM-DD)
                - startTime: the start time (convert to 24-hour HH:MM. Example: 10:30AM = 10:30)
                - endTime: the end time (convert to 24-hour HH:MM. Example: 10:30PM = 22:30)
                - location: where it takes place
                - applicationCloseDate: if mentioned (convert to YYYY-MM-DD)
                - isEmergency: true if emergency/urgent mentioned

                CRITICAL TIME RULES:
                - 10:30AM = 10:30
                - 10:30PM = 22:30
                - 9AM = 09:00
                - 12PM = 12:00

                Return ONLY JSON. No other text.";

            var jsonResponse = await CallOllama(prompt);
            jsonResponse = Regex.Replace(jsonResponse, @"```json\s*|\s*```", "");
            jsonResponse = jsonResponse.Trim();

            _logger.LogInformation($"LLM Response: {jsonResponse}");

            if (!string.IsNullOrEmpty(jsonResponse) && jsonResponse != "{}")
            {
                var extracted = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
                if (extracted != null)
                {
                    foreach (var kvp in extracted)
                    {
                        if (kvp.Value != null)
                        {
                            string value = kvp.Value.ToString() ?? string.Empty;
                            // Map field names to match your system
                            string key = kvp.Key switch
                            {
                                "title" => "Title",
                                "volunteers" => "Volunteers",
                                "date" => "Date",
                                "startTime" => "StartTime",
                                "endTime" => "EndTime",
                                "location" => "Location",
                                "applicationCloseDate" => "ApplicationCloseDate",
                                "isEmergency" => "IsEmergency",
                                _ => kvp.Key
                            };
                            result[key] = value;
                        }
                    }
                }
            }

            // Handle date conversions
            if (message.ToLower().Contains("tomorrow"))
            {
                result["Date"] = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                _logger.LogInformation($"Set date to tomorrow: {result["Date"]}");
            }
            else if (message.ToLower().Contains("today"))
            {
                result["Date"] = DateTime.Now.ToString("yyyy-MM-dd");
                _logger.LogInformation($"Set date to today: {result["Date"]}");
            }
            // Also ensure date is not in the past
            if (result.ContainsKey("Date") && DateTime.TryParse(result["Date"], out var parsedDate))
            {
                if (parsedDate < DateTime.Now.Date)
                {
                    _logger.LogWarning($"Date {parsedDate:yyyy-MM-dd} is in the past, resetting");
                    result.Remove("Date");
                }
            }
            if (result.ContainsKey("Date"))
            {
                string dateValue = result["Date"];
                // Check if date is like "20260820" (8 digits, no hyphens)
                if (dateValue.Length == 8 && !dateValue.Contains("-"))
                {
                    string year = dateValue.Substring(0, 4);
                    string month = dateValue.Substring(4, 2);
                    string day = dateValue.Substring(6, 2);
                    result["Date"] = $"{year}-{month}-{day}";
                    _logger.LogInformation($"Reformatted date: {dateValue} -> {result["Date"]}");
                }
            }
            if (result.ContainsKey("ApplicationCloseDate"))
            {
                string closeDateValue = result["ApplicationCloseDate"];
                if (closeDateValue.Length == 8 && !closeDateValue.Contains("-"))
                {
                    string year = closeDateValue.Substring(0, 4);
                    string month = closeDateValue.Substring(4, 2);
                    string day = closeDateValue.Substring(6, 2);
                    result["ApplicationCloseDate"] = $"{year}-{month}-{day}";
                    _logger.LogInformation($"Reformatted ApplicationCloseDate: {closeDateValue} -> {result["ApplicationCloseDate"]}");
                }
            }

            // Handle emergency detection if LLM missed it
            if (!result.ContainsKey("IsEmergency") && (message.ToLower().Contains("emergency") || message.ToLower().Contains("urgent")))
            {
                result["IsEmergency"] = "true";
            }
            // FORCE EMERGENCY DETECTION - check message directly
            if (message.ToLower().Contains("emergency") ||
                message.ToLower().Contains("emergency mode") ||
                message.ToLower().Contains("urgent"))
            {
                result["IsEmergency"] = "true";
                _logger.LogInformation("Force set IsEmergency to true from message content");
            }

            _logger.LogInformation($"Final extracted: {JsonSerializer.Serialize(result)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM extraction failed");
        }

        // SimpleExtract handle times (it already works correctly)
        var simpleExtract = SimpleExtract(message);

        // Override time fields with SimpleExtract results (they are more reliable)
        if (simpleExtract.ContainsKey("StartTime"))
        {
            result["StartTime"] = simpleExtract["StartTime"];
            _logger.LogInformation($"Using SimpleExtract StartTime: {result["StartTime"]}");
        }
        if (simpleExtract.ContainsKey("EndTime"))
        {
            result["EndTime"] = simpleExtract["EndTime"];
            _logger.LogInformation($"Using SimpleExtract EndTime: {result["EndTime"]}");
        }

        // For other fields, only use SimpleExtract if LLM didn't get them
        if (simpleExtract.ContainsKey("Title") && !result.ContainsKey("Title"))
            result["Title"] = simpleExtract["Title"];
        if (simpleExtract.ContainsKey("Volunteers") && !result.ContainsKey("Volunteers"))
            result["Volunteers"] = simpleExtract["Volunteers"];
        if (simpleExtract.ContainsKey("Date") && !result.ContainsKey("Date"))
            result["Date"] = simpleExtract["Date"];
        if (simpleExtract.ContainsKey("Location") && !result.ContainsKey("Location"))
            result["Location"] = simpleExtract["Location"];
        if (simpleExtract.ContainsKey("IsEmergency") && !result.ContainsKey("IsEmergency"))
            result["IsEmergency"] = simpleExtract["IsEmergency"];

        // Fallback to SimpleExtract if LLM returned nothing
        if (result.Count == 0)
        {
            _logger.LogInformation("LLM returned nothing, using SimpleExtract fallback");
            result = SimpleExtract(message);
        }

        return result;
    }

    private Dictionary<string, string> SimpleExtract(string message)
    {
        var lowerMsg = message.ToLower();
        var extracted = new Dictionary<string, string>();

        // Extract title (event name)
        string title = "";

        // Pattern: Look for event name at beginning or after "for"
        var forMatch = Regex.Match(message, @"for\s+([A-Za-z0-9\s\.]+?)(?=\s+(?:need|volunteer|at|from|on|tomorrow|today|help|$))", RegexOptions.IgnoreCase);
        if (forMatch.Success)
        {
            title = forMatch.Groups[1].Value.Trim();
        }

        if (string.IsNullOrEmpty(title))
        {
            var startMatch = Regex.Match(message, @"^([A-Za-z][A-Za-z0-9\s\.]+?)(?=\s+need|\s+volunteer)", RegexOptions.IgnoreCase);
            if (startMatch.Success)
            {
                title = startMatch.Groups[1].Value.Trim();
            }
        }

        if (!string.IsNullOrEmpty(title))
        {
            title = Regex.Replace(title, @"\b(?:need|volunteers?|for|at|from|to|with|and|the|help|out|on|tomorrow|today)\b", "", RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\s+", " ").Trim();
            if (!string.IsNullOrEmpty(title) && title.Length >= 2)
            {
                extracted["Title"] = title;
            }
        }

        // Extract volunteers
        var volMatch = Regex.Match(message, @"(\d+)\s*(?:volunteers|volunteer|people)", RegexOptions.IgnoreCase);
        if (volMatch.Success)
        {
            extracted["Volunteers"] = volMatch.Groups[1].Value;
        }

        // Extract date - handle DD/MM/YYYY format
        var dateMatch = Regex.Match(message, @"(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})");
        if (dateMatch.Success)
        {
            int day = int.Parse(dateMatch.Groups[1].Value);
            int month = int.Parse(dateMatch.Groups[2].Value);
            int year = int.Parse(dateMatch.Groups[3].Value);
            if (year < 100) year += 2000;

            try
            {
                var parsedDateValue = new DateTime(year, month, day);
                extracted["Date"] = parsedDateValue.ToString("yyyy-MM-dd");
            }
            catch
            {
                // Invalid date, try alternate format
                if (DateTime.TryParse($"{month}/{day}/{year}", out var altDate))
                {
                    extracted["Date"] = altDate.ToString("yyyy-MM-dd");
                }
            }
        }
        else if (lowerMsg.Contains("tomorrow"))
        {
            extracted["Date"] = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
        }
        else if (lowerMsg.Contains("today"))
        {
            extracted["Date"] = DateTime.Now.ToString("yyyy-MM-dd");
        }

        // Validate date is not in the past
        if (extracted.ContainsKey("Date") && DateTime.TryParse(extracted["Date"], out var checkedDate))
        {
            if (checkedDate < DateTime.Now.Date)
            {
                // If date is in the past, remove it (will be asked again)
                extracted.Remove("Date");
                _logger.LogInformation($"Removed past date: {checkedDate:yyyy-MM-dd}");
            }
        }

        // Extract Application Close Date
        if (message.ToLower().Contains("application closed date"))
        {
            var closeDateMatch = Regex.Match(message, @"(?:application\s+closed\s+date|application\s+close\s+date|closed\s+date)\s+(?:is|should\s+be\s+at\s+)?(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})", RegexOptions.IgnoreCase);
            if (closeDateMatch.Success)
            {
                int day = int.Parse(closeDateMatch.Groups[1].Value);
                int month = int.Parse(closeDateMatch.Groups[2].Value);
                int year = int.Parse(closeDateMatch.Groups[3].Value);
                if (year < 100) year += 2000;
                try
                {
                    var parsedDate = new DateTime(year, month, day);
                    extracted["ApplicationCloseDate"] = parsedDate.ToString("yyyy-MM-dd");
                    _logger.LogInformation($"Extracted ApplicationCloseDate: {extracted["ApplicationCloseDate"]}");
                }
                catch { }
            }
        }

        // Extract start time
        var timeMatch = Regex.Match(message, @"(\d{1,2})(?::(\d{2}))?\s*(?:am|pm)", RegexOptions.IgnoreCase);
        if (timeMatch.Success)
        {
            int hour = int.Parse(timeMatch.Groups[1].Value);
            int minute = timeMatch.Groups[2].Success ? int.Parse(timeMatch.Groups[2].Value) : 0;
            var period = Regex.Match(message, @"(am|pm)", RegexOptions.IgnoreCase).Value.ToLower();
            if (period == "pm" && hour != 12) hour += 12;
            else if (period == "am" && hour == 12) hour = 0;
            extracted["StartTime"] = $"{hour:D2}:{minute:D2}";
        }

        // Extract end time
        var endMatch = Regex.Match(message, @"(?:to|until)\s+(\d{1,2})(?::(\d{2}))?\s*(am|pm)", RegexOptions.IgnoreCase);
        if (endMatch.Success)
        {
            int hour = int.Parse(endMatch.Groups[1].Value);
            int minute = endMatch.Groups[2].Success ? int.Parse(endMatch.Groups[2].Value) : 0;
            var period = endMatch.Groups[3].Value.ToLower();
            if (period == "pm" && hour != 12) hour += 12;
            else if (period == "am" && hour == 12) hour = 0;
            extracted["EndTime"] = $"{hour:D2}:{minute:D2}";
            _logger.LogInformation($"Extracted EndTime: {extracted["EndTime"]}");
        }

        // Extract location
        var atMatch = Regex.Match(message, @"\bat\s+([A-Za-z][A-Za-z\s]+?)(?=\s+(?:from|to|$|and|\.))", RegexOptions.IgnoreCase);
        if (atMatch.Success)
        {
            extracted["Location"] = atMatch.Groups[1].Value.Trim();
        }
        else
        {
            var endAtMatch = Regex.Match(message, @"\bat\s+([A-Za-z][A-Za-z\s]+)$", RegexOptions.IgnoreCase);
            if (endAtMatch.Success)
            {
                extracted["Location"] = endAtMatch.Groups[1].Value.Trim();
            }
        }

        // Emergency detection
        var emergencyWords = new[] { "urgent", "emergency", "asap", "crisis", "disaster", "flood", "fire" };
        if (emergencyWords.Any(w => lowerMsg.Contains(w)))
        {
            extracted["IsEmergency"] = "true";
        }

        return extracted;
    }

    private bool IsPlaceholderValue(string value)
    {
        var placeholders = new[]
        {
            "event name", "Event Name", "YYYY-MM-DD", "HH:MM", "123 Main St",
            "Main St", "Unknown", "TBD", "tbd", "placeholder", "?", "event"
        };

        return string.IsNullOrEmpty(value) ||
               placeholders.Any(p => value.Equals(p, StringComparison.OrdinalIgnoreCase)) ||
               (value.Length < 2);
    }

    private string CleanTitle(string title)
    {
        title = Regex.Replace(title, @"\b(?:need|volunteers?|for|at|from|to|with|and|the|a|an|tomorrow|today|next|this|on|in)\b", "", RegexOptions.IgnoreCase);
        title = Regex.Replace(title, @"^\d+\s*", "");
        title = Regex.Replace(title, @"\s+", " ").Trim();

        if (!string.IsNullOrEmpty(title))
        {
            var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            title = textInfo.ToTitleCase(title.ToLower());
        }

        return title;
    }

    private string GenerateTemplateResponse(Dictionary<string, string> collectedData, string summary, bool isComplete)
    {
        // Clean up title in the response
        if (collectedData.ContainsKey("Title"))
        {
            string cleanTitle = collectedData["Title"];
            cleanTitle = Regex.Replace(cleanTitle, @"\b(tomorrow|today)\b", "", RegexOptions.IgnoreCase);
            cleanTitle = Regex.Replace(cleanTitle, @"\s+", " ").Trim();
            if (!string.IsNullOrEmpty(cleanTitle))
            {
                collectedData["Title"] = cleanTitle;
            }
        }

        if (isComplete)
        {
            return $@"✅ **Perfect! I've collected all the information needed!**

            You can now click the **'Create Opportunity'** button below to review and create your opportunity.

            {summary}";
        }

        var missingFields = GetMissingFields(collectedData);

        if (missingFields.Count > 0)
        {
            // Add helpful note about Application Close Date auto-calculation
            string autoHint = "";
            if (missingFields.Contains("ApplicationCloseDate") && collectedData.ContainsKey("Date") && !string.IsNullOrEmpty(collectedData["Date"]))
            {
                autoHint = "\n\n💡 **Hint:** Application Close Date will be auto-set to 1 day before Event Date if not provided.";
            }

            return $"📝 Please provide the following information: {string.Join(", ", missingFields)}.{autoHint}\n\n{summary}";
        }

        return $"📝 Got it! {summary}";
    }

    private List<string> GetMissingFields(Dictionary<string, string> data)
    {
        var required = new[] { "Volunteers", "Date", "StartTime", "EndTime", "Location" };
        var missing = new List<string>();

        foreach (var field in required)
        {
            if (!data.ContainsKey(field) || string.IsNullOrEmpty(data[field]))
            {
                missing.Add(field);
            }
        }

        return missing;
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class OllamaResponse
{
    public string response { get; set; } = string.Empty;
}

public class ConversationSession
{
    private List<(string role, string message)> _history = new();
    private Dictionary<string, string> _collectedData = new();
    private readonly ILogger _logger;

    public ConversationSession(ILogger logger = null)
    {
        _logger = logger;
    }

    public void AddMessage(string userMessage)
    {
        _history.Add(("user", userMessage));
    }

    public void AddAssistantMessage(string message)
    {
        _history.Add(("assistant", message));
    }

    public void UpdateExtractedData(Dictionary<string, string> data)
    {
        foreach (var item in data)
        {
            if (string.IsNullOrEmpty(item.Value) || item.Value == "null")
            {
                continue;
            }

            // SPECIAL HANDLING FOR TITLE - ALWAYS replace if new title is valid
            if (item.Key == "Title")
            {
                // Check if new title has letters and is valid
                if (Regex.IsMatch(item.Value, @"[A-Za-z]") && item.Value.Length >= 2)
                {
                    _collectedData[item.Key] = item.Value;
                    _logger?.LogInformation($"Replaced Title with: {item.Value}");
                }
                continue;
            }

            // For ApplicationCloseDate, only update if provided
            if (item.Key == "ApplicationCloseDate")
            {
                _collectedData[item.Key] = item.Value;
                _logger?.LogInformation($"Updated ApplicationCloseDate: {item.Value}");
                continue;
            }

            // For all other fields, update normally
            _collectedData[item.Key] = item.Value;
            _logger?.LogInformation($"Updated {item.Key} = {item.Value}");
        }
    }

    private bool IsPlaceholderValue(string value)
    {
        var placeholders = new[] { "YYYY-MM-DD", "HH:MM", "event name", "Event Name", "123 Main St", "?", "event" };
        return placeholders.Any(p => value.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    public List<(string role, string message)> GetConversationHistoryList()
    {
        return _history.TakeLast(10).ToList();
    }

    public Dictionary<string, string> GetCollectedData()
    {
        return new Dictionary<string, string>(_collectedData);
    }

    public string GetSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("\n📋 **Current Progress:**\n");

        // Show Title first
        if (_collectedData.ContainsKey("Title") &&
            !string.IsNullOrEmpty(_collectedData["Title"]) &&
            !IsPlaceholderValue(_collectedData["Title"]))
        {
            string cleanTitle = _collectedData["Title"];
            cleanTitle = Regex.Replace(cleanTitle, @"\b(tomorrow|today|need|volunteers?|for|at|from|to|on|help|out)\b", "", RegexOptions.IgnoreCase);
            cleanTitle = Regex.Replace(cleanTitle, @"\s+", " ").Trim();
            sb.AppendLine($"📌 Title: {cleanTitle}");
        }

        // Required fields in order
        var required = new List<(string key, string icon, string name)>
    {
        ("Volunteers", "👥", "Volunteers Needed"),
        ("Date", "📅", "Event Date"),
        ("ApplicationCloseDate", "📅", "Application Close Date"),
        ("StartTime", "⏰", "Start Time"),
        ("EndTime", "⏰", "End Time"),
        ("Location", "📍", "Location")
    };

        // Check if Date exists for auto-calculation message
        bool hasEventDate = _collectedData.ContainsKey("Date") &&
                            !string.IsNullOrEmpty(_collectedData["Date"]) &&
                            !IsPlaceholderValue(_collectedData["Date"]);

        foreach (var field in required)
        {
            if (_collectedData.ContainsKey(field.key) &&
                !string.IsNullOrEmpty(_collectedData[field.key]) &&
                !IsPlaceholderValue(_collectedData[field.key]))
            {
                // Format date nicely if it's a date field
                string value = _collectedData[field.key];
                if (field.key == "Date" || field.key == "ApplicationCloseDate")
                {
                    if (DateTime.TryParse(value, out var date))
                    {
                        value = date.ToString("dd/MM/yyyy");
                    }
                }
                sb.AppendLine($"✅ {field.icon} {field.name}: {value}");
            }
            else
            {
                // Special message for ApplicationCloseDate when not provided but Event Date exists
                if (field.key == "ApplicationCloseDate" && hasEventDate)
                {
                    if (DateTime.TryParse(_collectedData["Date"], out var eventDate))
                    {
                        var autoDate = eventDate.AddDays(-1);
                        sb.AppendLine($"ℹ️ {field.icon} {field.name}: Not provided (will be auto-set to {autoDate:dd/MM/yyyy} - 1 day before event)");
                    }
                    else
                    {
                        sb.AppendLine($"ℹ️ {field.icon} {field.name}: Not provided (will be auto-set to 1 day before event date)");
                    }
                }
                else
                {
                    sb.AppendLine($"❌ {field.icon} {field.name}: Not provided");
                }
            }
        }

        if (_collectedData.ContainsKey("IsEmergency") && _collectedData["IsEmergency"] == "true")
        {
            sb.AppendLine($"🚨 Emergency Mode: YES");
        }

        return sb.ToString();
    }

    public bool IsComplete()
    {
        var required = new[] { "Volunteers", "Date", "StartTime", "EndTime", "Location" };
        return required.All(f => _collectedData.ContainsKey(f) &&
                                !string.IsNullOrEmpty(_collectedData[f]) &&
                                !IsPlaceholderValue(_collectedData[f]));
    }
}

public class DescriptionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Volunteers { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsEmergency { get; set; }
}