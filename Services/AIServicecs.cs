using SchedulingWebApp.Models;
using SchedulingWebApp.Services;
using SchedulingWebApp.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class OpenAIClient: IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private List<Dictionary<string, string>> _conversationHistory = new();
    private int _schedulesInHistoryLimit;


    public OpenAIClient(string apiKey, int schedulesInHistoryLimit)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _schedulesInHistoryLimit = schedulesInHistoryLimit;
        AddAIPreset();
    }

    public async Task<string> SendRequestAsync(string prompt, Schedule schedule)
    {
        _conversationHistory.Add(new Dictionary<string, string>
        {
            ["role"] = "user",
            ["content"] = prompt
        });
        var requestBody = new
        {
            model = "gpt-4", // or "gpt-3.5-turbo"
            messages = _conversationHistory
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI request failed: {response.StatusCode} - {responseString}");
        }

        using var doc = JsonDocument.Parse(responseString);
        var result = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

        _conversationHistory.Remove(_conversationHistory.Last());

        return result; 
    }

    private void AddAIPreset()
    {
        _conversationHistory.Add (new Dictionary<string, string>
        {
            ["role"] = "system",
            ["content"] = "You are an expert scheduling assistant. Respond clearly and concisely. You will be asked questions about the scheduling process. You can answear only questions related to the scheduling" +
            $"process. You are given details about last planning horizons. If you're asked to compare the preference priorities of two or more employees, please direct users to the Reasoning Block tool located " +
            $"nearby for a clear explanation. Similarly, if questions arise about the reasoning behind certain decisions made by the scheduling model, refer users to the Reasoning Block for detailed insights."
        });
    }

    public string SaveScheduleContext(Schedule schedule)
    {
        string content = "";
        for(int e = 0; e < SchedulingService.NumEmployees; e++)
        {
            Employee emplopyee = SchedulingService.AllEmployees[e];
            content += $"{schedule.Name}:";
            content += $" <'{emplopyee.Name}': \n " +
                            $"|Assigned shifts| = [{GetAssignedShiftsText(schedule, e)}] \n" +
                            $"|Total preferences count| = {schedule.TotalPreferencesCount[e]} \n" +
                            $"|Satisfied preferences count| = {schedule.TotalPreferencesCount[e] - schedule.UnsatisfiedPreferencesCount[e]}\n" +
                            $"|Unsatisfied preferences count| = {schedule.UnsatisfiedPreferencesCount[e]}\n" +
                            $"|Role| = {emplopyee.Role.ToString()} \n" +
                            $"|Unsatisfaction percantage| = {schedule.UnsatisfactionPercentage[e]}\n" +
                            $"|Role percantage| = {schedule.RolePercentage[e]}\n" +
                            $"|Unsatisfaction streak| = {schedule.UnsatisfactionStreak[e]}>\n";
                            


        }
        if (_conversationHistory.Count >= _schedulesInHistoryLimit + 1) {
            _conversationHistory.RemoveAt(1);
        }
        _conversationHistory.Add(new Dictionary<string, string>
        {
            ["role"] = "system",
            ["content"] = content
        });

        return content;
    }

    private string GetAssignedShiftsText(Schedule schedule, int e)
    {
        string text = "";
        for(int d = 0; d < SchedulingService.NumDays; d++)
        {
            text += $"{SchedulingService.AllDays[d].Name}: ";
            for(int s = 0; s < SchedulingService.NumShifts; s++)
            {
                text += $"{SchedulingService.AllShifts[s].Name}";
                if (schedule.SchedulingResult.ShiftsAssingment[e, d, s])
                {
                    text += $"(Is assigned, ";
                }
                else
                {
                    text += $"(Is not assigned, ";
                }

                if (schedule.ShiftPreferences[e, d, s])
                {
                    text += $"shift was preffered);";
                }
                else
                {
                    text += $"shift was not preffered);";
                }
            }
            text += "/n";
        }
        return text;
    }
}