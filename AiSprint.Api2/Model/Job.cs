// A job posting as stored in jobs.json.
public record Job(
    int Id,
    string Title,
    string Company,
    bool Remote,
    string[] Skills,
    string Description);
