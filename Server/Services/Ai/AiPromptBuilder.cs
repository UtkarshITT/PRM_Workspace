using System.Text;
using System.Text.Json;
using PRM.Server.Services.Ai.Models;

namespace PRM.Server.Services.Ai;

public static class AiPromptBuilder
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true
	};

	public static string BuildSkillMatchPrompt(string requirement, IReadOnlyList<AiCandidateContext> candidates)
	{
		var context = new AiSkillMatchPromptContext
		{
			Requirement = requirement,
			Candidates = candidates.Take(30).Select(ToJsonCandidate).ToList()
		};

		var sb = new StringBuilder();
		sb.AppendLine("You are a resource matching assistant for an IT services company.");
		sb.AppendLine("Answer only from the JSON context. Do not invent employees, skills, availability, or hours.");
		sb.AppendLine("Write concise, manager-friendly reasons.");
		sb.AppendLine("Respond with JSON only using this schema:");
		sb.AppendLine("""
			{
			  "summary": "Short manager-friendly summary.",
			  "gapAnalysis": ["Optional gap or availability note."],
			  "candidates": [
			    { "employeeId": 101, "rank": 1, "matchScore": 92, "reason": "..." }
			  ]
			}
			""");
		sb.AppendLine();
		sb.AppendLine("JSON context:");
		sb.AppendLine(JsonSerializer.Serialize(context, JsonOptions));
		return sb.ToString();
	}

	public static string BuildRiskSummaryPrompt(ProjectAiContext context)
	{
		var sb = new StringBuilder();
		sb.AppendLine("You are a project risk analyst. Write one plain-English paragraph about project health risks.");
		sb.AppendLine("Use only the data below. Do not invent people or milestones.");
		sb.AppendLine("Respond with JSON: { \"paragraph\": \"...\", \"confidence\": \"medium\" }");
		sb.AppendLine();
		sb.AppendLine($"Project: {context.ProjectName}");
		sb.AppendLine($"Health status: {context.HealthStatus}");
		sb.AppendLine($"End date: {context.EndDate:yyyy-MM-dd}");
		sb.AppendLine();
		sb.AppendLine("Milestones:");
		foreach (var milestone in context.Milestones)
		{
			sb.AppendLine($"- {milestone.Title}: due {milestone.DueDate:yyyy-MM-dd}, status {milestone.Status}, overdue={milestone.IsOverdue}");
		}

		sb.AppendLine();
		sb.AppendLine("Last week hours vs expected:");
		foreach (var hours in context.LastWeekHours)
		{
			sb.AppendLine($"- {hours.EmployeeName}: {hours.LoggedHours:0.#}h logged vs {hours.ExpectedHours:0.#}h expected");
		}

		if (context.RiskFlags.Count > 0)
		{
			sb.AppendLine();
			sb.AppendLine("Risk flags:");
			foreach (var flag in context.RiskFlags)
			{
				sb.AppendLine($"- {flag}");
			}
		}

		return sb.ToString();
	}

	public static string BuildTeamBuilderPrompt(
		IReadOnlyList<TeamRolePromptContext> roles,
		IReadOnlyDictionary<string, IReadOnlyList<AiCandidateContext>> candidatesByRole)
	{
		var context = new
		{
			scope = "Organization",
			roles,
			candidatesByRole = candidatesByRole.ToDictionary(
				item => item.Key,
				item => item.Value.Take(30).Select(ToJsonCandidate).ToList(),
				StringComparer.OrdinalIgnoreCase)
		};

		var sb = new StringBuilder();
		sb.AppendLine("You are a team-building assistant. For each role, rank the best matches from the provided candidates.");
		sb.AppendLine("Use only employeeId values from the JSON context. Respond with JSON only:");
		sb.AppendLine("""
			{
			  "summary": "Short manager-friendly summary.",
			  "gapAnalysis": ["Optional gap or availability note."],
			  "roles": [
			    {
			      "roleTitle": "Backend Lead",
			      "headcount": 1,
			      "candidates": [
			        { "employeeId": 101, "rank": 1, "matchScore": 90, "reason": "..." }
			      ]
			    }
			  ]
			}
			""");
		sb.AppendLine();
		sb.AppendLine("JSON context:");
		sb.AppendLine(JsonSerializer.Serialize(context, JsonOptions));
		return sb.ToString();
	}

	public static string BuildTeamBuilderPromptFromNaturalLanguage(
		string managerRequest,
		IReadOnlyList<AiCandidateContext> fullyAvailableCandidates,
		IReadOnlyList<AiCandidateContext> partiallyAvailableCandidates,
		IReadOnlyList<AiCandidateContext> fullyAllocatedCandidates)
	{
		var context = new AiTeamBuilderPromptContext
		{
			ManagerRequest = managerRequest,
			FullyAvailableCandidates = fullyAvailableCandidates.Take(50).Select(ToJsonCandidate).ToList(),
			PartiallyAvailableCandidates = partiallyAvailableCandidates.Take(50).Select(ToJsonCandidate).ToList(),
			FullyAllocatedCandidates = fullyAllocatedCandidates.Take(50).Select(ToJsonCandidate).ToList()
		};

		var sb = new StringBuilder();
		sb.AppendLine("You are a team-building assistant for an IT services company.");
		sb.AppendLine("Infer the required roles and headcount from the manager request.");
		sb.AppendLine("Use only fullyAvailableCandidates for selectable matches.");
		sb.AppendLine("Use partiallyAvailableCandidates and fullyAllocatedCandidates only for gap analysis.");
		sb.AppendLine("For every selected candidate, explain why that employee was chosen before other available or unavailable matches.");
		sb.AppendLine("Group selected resources under the role they satisfy. Do not return one generic team bucket if roles can be inferred.");
		sb.AppendLine("If matching skills exist but candidates are not fully available, explain that clearly.");
		sb.AppendLine("If a requested skill is absent from all candidate groups, suggest training or hiring.");
		sb.AppendLine("Respond with JSON only using this schema:");
		sb.AppendLine("""
			{
			  "summary": "Short manager-friendly summary.",
			  "gapAnalysis": ["Resource gap, availability, training, or hiring note."],
			  "roles": [
			    {
			      "roleTitle": "Developer",
			      "headcount": 4,
			      "candidates": [
			        {
			          "employeeId": 101,
			          "rank": 1,
			          "matchScore": 90,
			          "reason": "Why selected for this role and why ahead of other available/unavailable matches."
			        }
			      ]
			    }
			  ]
			}
			""");
		sb.AppendLine();
		sb.AppendLine("JSON context:");
		sb.AppendLine(JsonSerializer.Serialize(context, JsonOptions));
		return sb.ToString();
	}

	private static AiCandidateJsonContext ToJsonCandidate(AiCandidateContext candidate)
	{
		return new AiCandidateJsonContext
		{
			EmployeeId = candidate.Id,
			FullName = candidate.FullName,
			Department = candidate.Department,
			ManagerName = candidate.ManagerName,
			EmploymentStatus = candidate.EmploymentStatus,
			CurrentUtilizationPercent = candidate.CurrentUtilization,
			AvailabilityPercent = candidate.AvailabilityPercent,
			Skills = candidate.Skills,
			RecentActivityTags = candidate.RecentActivityTags
		};
	}
}
