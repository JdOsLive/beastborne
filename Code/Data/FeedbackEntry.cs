using System.Collections.Generic;

namespace Beastborne.Data;

public class FeedbackEntry
{
	public int Id { get; set; }
	public string Type { get; set; }
	public string Tag { get; set; }
	public string Title { get; set; }
	public string Body { get; set; }
	public int Votes { get; set; }
	public bool VotedByMe { get; set; }
	public bool Resolved { get; set; }
	public string ResolvedNote { get; set; }
	public string AuthorSteamId { get; set; }
	public string AuthorName { get; set; }
	public string CreatedAt { get; set; }
	public string UpdatedAt { get; set; }
}

public class FeedbackListResponse
{
	public List<FeedbackEntry> Entries { get; set; } = new();
}

public class FeedbackCreateRequest
{
	public string Type { get; set; }
	public string Tag { get; set; }
	public string Title { get; set; }
	public string Body { get; set; }
	public string AuthorName { get; set; }
}

public class FeedbackVoteResponse
{
	public int Votes { get; set; }
	public bool VotedByMe { get; set; }
}

public class FeedbackPatchRequest
{
	public bool? Resolved { get; set; }
	public string ResolvedNote { get; set; }
	public string Tag { get; set; }
	public string Type { get; set; }
}

public class FeedbackSuccessResponse
{
	public bool Success { get; set; }
}
