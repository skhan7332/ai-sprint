using System.ComponentModel;
using System.Text.Json;

namespace AiSprint.Api2.Model
{
    public class JobTools
    {
        private List<Job> jobs = [];
        public JobTools(string jsonFile)
        {
            if (File.Exists(jsonFile))
            {
                var jsonData = File.ReadAllText(jsonFile);
                jobs = JsonSerializer.Deserialize<List<Job>>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.ToList() ?? new List<Job>();
            }
            else
            {
                throw new FileNotFoundException("json missing");
            }
        }

        [Description("Searches job postings by keyword. Returns id, title, company and whether it is remote for each match.")]
        public string SearchJobs(
        [Description("A single word or skill to look for, e.g. 'React', 'Java' or 'Azure'.")]
        string keyWord)
        {
            Console.WriteLine($"[tool] SearchJobs(\"{keyWord}\")");

            var matches = jobs.Where(j =>
                j.Title.Contains(keyWord, StringComparison.OrdinalIgnoreCase) ||
                j.Company.Contains(keyWord, StringComparison.OrdinalIgnoreCase) ||
                j.Description.Contains(keyWord, StringComparison.OrdinalIgnoreCase) ||
                j.Skills.Any(s => s.Contains(keyWord, StringComparison.OrdinalIgnoreCase)));

            var lines = matches.Select(FormatJob).ToList();

            return lines.Count > 0
                ? string.Join("\n", lines)
                : $"No jobs found matching '{keyWord}'. Tell the user there are no matches. " +
                  "Do not invent jobs; to suggest alternatives, call ListAllJobs and pick from its results.";
        }

        [Description("Lists every available job posting. Returns id, title, company and whether it is remote for each job. " +
                     "Use it to suggest alternatives when a search finds nothing, or when the user asks what jobs exist.")]
        public string ListAllJobs()
        {
            Console.WriteLine("[tool] ListAllJobs()");
            return string.Join("\n", jobs.Select(FormatJob));
        }

        [Description("Gets the full details of one job posting by its id: title, company, remote, required skills and description. " +
                     "Use it before reviewing a job's fit, since search results don't include the description.")]
        public string GetJobDetails(
            [Description("The job id, as returned by SearchJobs or ListAllJobs.")] int id)
        {
            Console.WriteLine($"[tool] GetJobDetails({id})");

            Job? job = jobs.FirstOrDefault(j => j.Id == id);
            if (job is null)
                return $"No job found with id {id}. Use SearchJobs or ListAllJobs to find valid ids.";

            return $"""
        Id: {job.Id}
        Title: {job.Title}
        Company: {job.Company}
        Remote: {(job.Remote ? "Yes" : "No")}
        Skills: {string.Join(", ", job.Skills)}
        Description: {job.Description}
        """;
        }

        private string FormatJob(Job j) => $"{j.Id} | {j.Title} | {j.Company} | {(j.Remote ? "Remote" : "On-site")}";
    }
}
