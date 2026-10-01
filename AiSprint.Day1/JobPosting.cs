using System.ComponentModel;

[Description("Structured details extracted from a job posting.")]
public record JobPosting(
    [property: Description("The job title, e.g. 'Senior .NET Developer'.")]
    string Title,

    [property: Description("Technical skills explicitly required by the posting, e.g. 'C#', 'Azure', 'SQL'.")]
    string[] RequiredSkills,

    [property: Description("True if the role can be done fully remotely; false if on-site or hybrid.")]
    bool IsRemote,

    [property: Description("Minimum years of experience required, or null if not stated.")]
    int? MinYearsExperience)
{
    // Records print arrays as "System.String[]", so format the skills explicitly.
    public override string ToString() =>
        $"{Title} | Remote: {(IsRemote ? "Yes" : "No")} | " +
        $"Experience: {(MinYearsExperience is int years ? $"{years}+ yrs" : "Not specified")} | " +
        $"Skills: {(RequiredSkills.Length > 0 ? string.Join(", ", RequiredSkills) : "None")}";
}

// Sample postings for testing extraction: raw text to send to the model, plus the record we expect back.
public static class JobPostingSamples
{
    public static readonly (string RawText, JobPosting Expected)[] All =
    [
        // Straightforward: everything stated clearly.
        (
            """
            Senior .NET Developer — Fully Remote
            We're looking for a Senior .NET Developer with 5+ years of professional experience.
            Required: C#, ASP.NET Core, Entity Framework Core, SQL Server, Azure.
            You'll design and maintain APIs serving millions of requests per day.
            """,
            new("Senior .NET Developer", ["C#", "ASP.NET Core", "Entity Framework Core", "SQL Server", "Azure"], true, 5)
        ),

        // On-site, no experience requirement mentioned -> MinYearsExperience should be null.
        (
            """
            Junior Frontend Developer (On-site, Bangalore)
            Join our product team in our Bangalore office. Freshers welcome!
            Must know: JavaScript, React, HTML, CSS.
            """,
            new("Junior Frontend Developer", ["JavaScript", "React", "HTML", "CSS"], false, null)
        ),

        // Hybrid counts as not remote; experience written as a word.
        (
            """
            DevOps Engineer
            Location: Hybrid — 3 days a week in our Pune office.
            You should have at least three years of experience running production infrastructure.
            Required skills: Docker, Kubernetes, Terraform, GitHub Actions, Linux.
            """,
            new("DevOps Engineer", ["Docker", "Kubernetes", "Terraform", "GitHub Actions", "Linux"], false, 3)
        ),

        // Experience given as a range -> minimum is the lower bound.
        (
            """
            Data Engineer (Remote within India)
            Experience: 3-6 years.
            Requirements: Python, Apache Spark, SQL, Airflow.
            Work from anywhere in India.
            """,
            new("Data Engineer", ["Python", "Apache Spark", "SQL", "Airflow"], true, 3)
        ),

        // Trap: "nice to have" skills must NOT be included in RequiredSkills.
        (
            """
            Backend Engineer — Remote-first company
            Requirements:
            - 2+ years building backend services
            - Strong Go and PostgreSQL skills
            Nice to have: Kafka, gRPC, AWS.
            """,
            new("Backend Engineer", ["Go", "PostgreSQL"], true, 2)
        ),

        // Messy, informal posting with skills buried in prose.
        (
            """
            hey folks!! we're hiring a full stack dev at our startup 🚀
            office is in Hyderabad, need you there mon-fri. ideally you've shipped stuff
            for like 4 yrs or so. our stack is angular on the front, .net 8 + c# on the back,
            and we deploy everything to azure. DM me!
            """,
            new("Full Stack Developer", ["Angular", ".NET 8", "C#", "Azure"], false, 4)
        ),
    ];
}
