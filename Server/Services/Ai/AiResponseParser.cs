using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PRM.Server.Models.DTOs.Ai;

namespace PRM.Server.Services.Ai;

public static class AiResponseParser
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		NumberHandling = JsonNumberHandling.AllowReadingFromString
	};

	public static AiSkillMatchResponseDto ParseSkillMatch(string raw, HashSet<long> eligibleIds)
	{
		var parsed = TryDeserialize<SkillMatchJson>(raw);
		var candidates = parsed?.Candidates ?? [];

		var validated = candidates
			.Where(candidate => eligibleIds.Contains(candidate.EmployeeId))
			.OrderBy(candidate => candidate.Rank)
			.Take(5)
			.Select(candidate => new AiSkillMatchCandidateDto
			{
				EmployeeId = candidate.EmployeeId,
				Rank = candidate.Rank,
				MatchScore = candidate.MatchScore,
				Reason = candidate.Reason ?? string.Empty
			})
			.ToList();

		return new AiSkillMatchResponseDto
		{
			Summary = parsed?.Summary,
			GapAnalysis = parsed?.GapAnalysis ?? [],
			Candidates = validated,
			AiGenerated = validated.Count > 0,
			Disclaimer = "AI-generated. Verify before confirming."
		};
	}

	public static string ParseRiskSummary(string raw)
	{
		var parsed = TryDeserialize<RiskSummaryJson>(raw);
		if (!string.IsNullOrWhiteSpace(parsed?.Paragraph))
		{
			return parsed.Paragraph.Trim();
		}

		var stripped = StripCodeFences(raw).Trim();
		if (stripped.StartsWith('{'))
		{
			var retry = TryDeserialize<RiskSummaryJson>(stripped);
			if (!string.IsNullOrWhiteSpace(retry?.Paragraph))
			{
				return retry.Paragraph.Trim();
			}
		}

		return stripped;
	}

	public static Dictionary<string, List<AiSkillMatchCandidateDto>> ParseTeamBuilder(
		string raw,
		IReadOnlyDictionary<string, HashSet<long>> eligibleByRole)
	{
		var parsed = TryDeserialize<TeamBuilderJson>(raw);
		var result = new Dictionary<string, List<AiSkillMatchCandidateDto>>(StringComparer.OrdinalIgnoreCase);

		if (parsed?.Roles == null)
		{
			return result;
		}

		foreach (var role in parsed.Roles)
		{
			if (string.IsNullOrWhiteSpace(role.RoleTitle) || !eligibleByRole.TryGetValue(role.RoleTitle, out var eligibleIds))
			{
				continue;
			}

			var matches = (role.Candidates ?? [])
				.Where(candidate => eligibleIds.Contains(candidate.EmployeeId))
				.OrderBy(candidate => candidate.Rank)
				.Take(role.Headcount > 0 ? role.Headcount : 1)
				.Select(candidate => new AiSkillMatchCandidateDto
				{
					EmployeeId = candidate.EmployeeId,
					Rank = candidate.Rank,
					MatchScore = candidate.MatchScore,
					Reason = candidate.Reason ?? string.Empty
				})
				.ToList();

			result[role.RoleTitle] = matches;
		}

		return result;
	}

	public static TeamBuilderResponseDto ParseTeamBuilderFromPrompt(string raw, HashSet<long> eligibleIds)
	{
		var parsed = TryDeserialize<TeamBuilderJson>(raw);
		if (parsed?.Roles == null)
		{
			return new TeamBuilderResponseDto();
		}

		return new TeamBuilderResponseDto
		{
			Summary = parsed.Summary,
			GapAnalysis = parsed.GapAnalysis ?? [],
			AiGenerated = true,
			Roles = parsed.Roles
			.Where(role => !string.IsNullOrWhiteSpace(role.RoleTitle))
			.Select(role => new TeamBuilderRoleResultDto
			{
				RoleTitle = role.RoleTitle!.Trim(),
				Headcount = role.Headcount,
				Matches = (role.Candidates ?? [])
					.Where(candidate => eligibleIds.Contains(candidate.EmployeeId))
					.OrderBy(candidate => candidate.Rank)
					.Take(role.Headcount > 0 ? role.Headcount : 1)
					.Select(candidate => new AiSkillMatchCandidateDto
					{
						EmployeeId = candidate.EmployeeId,
						Rank = candidate.Rank,
						MatchScore = candidate.MatchScore,
						Reason = candidate.Reason ?? string.Empty
					})
					.ToList()
			})
			.ToList()
		};
	}

	private static T? TryDeserialize<T>(string raw)
	{
		var text = StripCodeFences(raw);
		try
		{
			return JsonSerializer.Deserialize<T>(text, JsonOptions);
		}
		catch (JsonException)
		{
			return default;
		}
	}

	private static string StripCodeFences(string raw)
	{
		var trimmed = raw.Trim();
		var match = Regex.Match(trimmed, @"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
		return match.Success ? match.Groups[1].Value.Trim() : trimmed;
	}

	private sealed class SkillMatchJson
	{
		public string? Summary { get; set; }
		public List<string>? GapAnalysis { get; set; }
		public List<SkillMatchCandidateJson>? Candidates { get; set; }
	}

	private sealed class SkillMatchCandidateJson
	{
		public long EmployeeId { get; set; }
		public int Rank { get; set; }
		public int MatchScore { get; set; }
		public string? Reason { get; set; }
	}

	private sealed class RiskSummaryJson
	{
		public string? Paragraph { get; set; }
	}

	private sealed class TeamBuilderJson
	{
		public string? Summary { get; set; }
		public List<string>? GapAnalysis { get; set; }
		public List<TeamBuilderRoleJson>? Roles { get; set; }
	}

	private sealed class TeamBuilderRoleJson
	{
		public string? RoleTitle { get; set; }
		public int Headcount { get; set; } = 1;
		public List<SkillMatchCandidateJson>? Candidates { get; set; }
	}
}
