using System.Text.RegularExpressions;
using PRM.Server.Constants;
using PRM.Server.Exceptions;
using PRM.Server.Helpers;
using PRM.Server.Models.DTOs.Ai;
using PRM.Server.Models.Entities;
using PRM.Server.Repositories.Interfaces;
using PRM.Server.Services.Ai;
using PRM.Server.Services.Ai.Models;

namespace PRM.Server.Services.Interfaces;

public interface IAiIntegrationService
{
	Task<AiSkillMatchResponseDto> GetSkillMatchAsync(string requirement, long managerUserId, CancellationToken cancellationToken = default);
	Task<AiRiskSummaryResponseDto> GetProjectRiskSummaryAsync(long projectId, long managerUserId, CancellationToken cancellationToken = default);
	Task<TeamBuilderResponseDto> BuildTeamAsync(TeamBuilderRequestDto request, long managerUserId, CancellationToken cancellationToken = default);
}

public class AiIntegrationService : IAiIntegrationService
{
	private const int MaxAiRequestsPerHour = 10;
	private const decimal DefaultMaxWeeklyHours = 40;
	private const string FallbackSkillMatchMessage = "AI service unavailable. Please use direct search on the Resource Dashboard.";
	private const string FallbackRiskMessage = "AI risk summary unavailable. Review milestones and team timesheets manually.";
	private const string FallbackTeamBuilderMessage = "AI team builder unavailable. Review team skills on the Resource Dashboard.";

	private readonly IResourceProfileRepository _resourceProfileRepository;
	private readonly IProjectRepository _projectRepository;
	private readonly ITimesheetRepository _timesheetRepository;
	private readonly ISystemConfigRepository _systemConfigRepository;
	private readonly IAiRequestLogRepository _aiRequestLogRepository;
	private readonly ISkillRepository _skillRepository;
	private readonly ILlmClientFactory _llmClientFactory;
	private readonly ILogger<AiIntegrationService> _logger;

	public AiIntegrationService(
		IResourceProfileRepository resourceProfileRepository,
		IProjectRepository projectRepository,
		ITimesheetRepository timesheetRepository,
		ISystemConfigRepository systemConfigRepository,
		IAiRequestLogRepository aiRequestLogRepository,
		ISkillRepository skillRepository,
		ILlmClientFactory llmClientFactory,
		ILogger<AiIntegrationService> logger)
	{
		_resourceProfileRepository = resourceProfileRepository;
		_projectRepository = projectRepository;
		_timesheetRepository = timesheetRepository;
		_systemConfigRepository = systemConfigRepository;
		_aiRequestLogRepository = aiRequestLogRepository;
		_skillRepository = skillRepository;
		_llmClientFactory = llmClientFactory;
		_logger = logger;
	}

	public async Task<AiSkillMatchResponseDto> GetSkillMatchAsync(
		string requirement,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(requirement))
		{
			throw new ValidationException("Requirement text is required.");
		}

		await EnforceRateLimitAsync(managerUserId, cancellationToken);

		var candidates = await BuildOrganizationCandidatesAsync(cancellationToken);
		if (candidates.Count == 0)
		{
			return new AiSkillMatchResponseDto
			{
				Message = "No active employees are available in the organization resource pool.",
				AiGenerated = false,
				Disclaimer = string.Empty
			};
		}

		var eligible = candidates.Where(candidate => candidate.AvailabilityPercent > 0).ToList();

		if (eligible.Count == 0)
		{
			return new AiSkillMatchResponseDto
			{
				Message = "No eligible candidates available. All active employees are at 100% utilization.",
				AiGenerated = false,
				Disclaimer = string.Empty,
				GapAnalysis =
				[
					"The organization has resources, but they are fully allocated right now. Review allocation end dates or plan hiring/training for this demand."
				]
			};
		}

		var (provider, apiKey) = await _systemConfigRepository.GetLlmSettingsAsync(cancellationToken);
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return BuildDeterministicSkillMatch(requirement, eligible);
		}

		var prompt = AiPromptBuilder.BuildSkillMatchPrompt(requirement, eligible);
		var eligibleIds = eligible.Select(candidate => candidate.Id).ToHashSet();

		try
		{
			var llmClient = _llmClientFactory.Create(provider);
			var raw = await CallLlmWithRetryAsync(llmClient, prompt, apiKey, cancellationToken);
			var parsed = AiResponseParser.ParseSkillMatch(raw, eligibleIds);
			EnrichCandidateNames(parsed, eligible);

			await LogAiRequestAsync("SKILL_MATCH", prompt, raw, managerUserId, cancellationToken);

			if (parsed.Candidates.Count == 0)
			{
				return BuildDeterministicSkillMatch(requirement, eligible);
			}

			parsed.AiGenerated = true;
			return parsed;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "LLM skill match failed for manager {ManagerUserId}", managerUserId);
			return new AiSkillMatchResponseDto
			{
				Message = FallbackSkillMatchMessage,
				AiGenerated = false,
				Disclaimer = string.Empty
			};
		}
	}

	public async Task<AiRiskSummaryResponseDto> GetProjectRiskSummaryAsync(
		long projectId,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		await EnforceRateLimitAsync(managerUserId, cancellationToken);

		var project = await _projectRepository.GetDetailByIdAsync(projectId, cancellationToken);

		if (project == null)
		{
			throw new NotFoundException($"Project with ID {projectId} was not found.");
		}

		if (project.ManagerUserId != managerUserId)
		{
			throw new ValidationException("You can only request risk summaries for your own projects.");
		}

		var context = await BuildProjectAiContextAsync(project, cancellationToken);
		var (provider, apiKey) = await _systemConfigRepository.GetLlmSettingsAsync(cancellationToken);

		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return BuildTemplateRiskSummary(context);
		}

		var prompt = AiPromptBuilder.BuildRiskSummaryPrompt(context);

		try
		{
			var llmClient = _llmClientFactory.Create(provider);
			var raw = await CallLlmWithRetryAsync(llmClient, prompt, apiKey, cancellationToken);
			var paragraph = AiResponseParser.ParseRiskSummary(raw);

			if (string.IsNullOrWhiteSpace(paragraph))
			{
				return BuildTemplateRiskSummary(context);
			}

			await LogAiRequestAsync("RISK_SUMMARY", prompt, paragraph, managerUserId, cancellationToken);
			await _projectRepository.UpdateLastRiskSummaryAsync(projectId, paragraph, cancellationToken);

			return new AiRiskSummaryResponseDto
			{
				Paragraph = paragraph,
				AiGenerated = true,
				ProjectName = context.ProjectName,
				HealthStatus = context.HealthStatus
			};
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "LLM risk summary failed for project {ProjectId}", projectId);
			return new AiRiskSummaryResponseDto
			{
				Message = FallbackRiskMessage,
				AiGenerated = false,
				ProjectName = context.ProjectName,
				HealthStatus = context.HealthStatus
			};
		}
	}

	public async Task<TeamBuilderResponseDto> BuildTeamAsync(
		TeamBuilderRequestDto request,
		long managerUserId,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(request.Prompt) && request.Roles.Count == 0)
		{
			throw new ValidationException("Enter a team request prompt or at least one role.");
		}

		await EnforceRateLimitAsync(managerUserId, cancellationToken);

		if (request.ProjectId.HasValue)
		{
			var project = await _projectRepository.GetByIdAsync(request.ProjectId.Value, cancellationToken);

			if (project == null || project.ManagerUserId != managerUserId)
			{
				throw new ValidationException("You can only build teams for your own projects.");
			}
		}

		var allCandidates = await BuildOrganizationCandidatesAsync(cancellationToken);
		if (!string.IsNullOrWhiteSpace(request.Prompt))
		{
			return await BuildTeamFromNaturalLanguageAsync(request.Prompt, allCandidates, managerUserId, cancellationToken);
		}

		var skillMap = await LoadSkillMapAsync(request.Roles, cancellationToken);
		var roleContexts = new List<TeamRolePromptContext>();
		var candidatesByRole = new Dictionary<string, IReadOnlyList<AiCandidateContext>>(StringComparer.OrdinalIgnoreCase);
		var eligibleByRole = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);

		foreach (var role in request.Roles)
		{
			var skillNames = role.SkillIds
				.Where(skillMap.ContainsKey)
				.Select(skillId => skillMap[skillId])
				.ToList();

			var roleEligible = allCandidates
				.Where(candidate => MeetsRoleRequirements(candidate, role, skillMap))
				.Where(candidate => candidate.AvailabilityPercent >= 100)
				.ToList();

			roleContexts.Add(new TeamRolePromptContext
			{
				RoleTitle = role.RoleTitle,
				SkillNames = skillNames,
				MinProficiency = role.MinProficiency,
				AllocationPercent = role.AllocationPercent,
				Headcount = role.Headcount
			});

			candidatesByRole[role.RoleTitle] = roleEligible;
			eligibleByRole[role.RoleTitle] = roleEligible.Select(candidate => candidate.Id).ToHashSet();
		}

		if (roleContexts.All(role => candidatesByRole[role.RoleTitle].Count == 0))
		{
			return new TeamBuilderResponseDto
			{
				Message = "No eligible candidates match the requested roles with sufficient availability.",
				AiGenerated = false,
				Disclaimer = string.Empty,
				GapAnalysis = BuildRoleGapAnalysis(request.Roles, allCandidates, skillMap)
			};
		}

		var (provider, apiKey) = await _systemConfigRepository.GetLlmSettingsAsync(cancellationToken);
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return BuildDeterministicTeamBuilder(request.Roles, candidatesByRole);
		}

		var prompt = AiPromptBuilder.BuildTeamBuilderPrompt(roleContexts, candidatesByRole);

		try
		{
			var llmClient = _llmClientFactory.Create(provider);
			var raw = await CallLlmWithRetryAsync(llmClient, prompt, apiKey, cancellationToken);
			var parsedRoles = AiResponseParser.ParseTeamBuilder(raw, eligibleByRole);

			await LogAiRequestAsync("TEAM_BUILDER", prompt, raw, managerUserId, cancellationToken);

			var response = new TeamBuilderResponseDto { AiGenerated = true };
			foreach (var role in request.Roles)
			{
				var matches = parsedRoles.TryGetValue(role.RoleTitle, out var roleMatches) && roleMatches.Count > 0
					? roleMatches
					: BuildDeterministicMatches(role, candidatesByRole.GetValueOrDefault(role.RoleTitle) ?? []);

				EnrichCandidateNames(new AiSkillMatchResponseDto { Candidates = matches }, allCandidates);

				response.Roles.Add(new TeamBuilderRoleResultDto
				{
					RoleTitle = role.RoleTitle,
					Headcount = role.Headcount,
					Matches = matches
				});
			}

			return response;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "LLM team builder failed for manager {ManagerUserId}", managerUserId);
			return new TeamBuilderResponseDto
			{
				Message = FallbackTeamBuilderMessage,
				AiGenerated = false
			};
		}
	}

	private async Task<TeamBuilderResponseDto> BuildTeamFromNaturalLanguageAsync(
		string managerRequest,
		IReadOnlyList<AiCandidateContext> allCandidates,
		long managerUserId,
		CancellationToken cancellationToken)
	{
		var fullyAvailableCandidates = allCandidates
			.Where(candidate => candidate.AvailabilityPercent >= 100)
			.ToList();
		var partiallyAvailableCandidates = allCandidates
			.Where(candidate => candidate.AvailabilityPercent > 0 && candidate.AvailabilityPercent < 100)
			.ToList();
		var fullyAllocatedCandidates = allCandidates
			.Where(candidate => candidate.AvailabilityPercent <= 0)
			.ToList();

		var (provider, apiKey) = await _systemConfigRepository.GetLlmSettingsAsync(cancellationToken);
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return BuildDeterministicTeamBuilderFromPrompt(
				managerRequest,
				fullyAvailableCandidates,
				partiallyAvailableCandidates,
				fullyAllocatedCandidates);
		}

		var prompt = AiPromptBuilder.BuildTeamBuilderPromptFromNaturalLanguage(
			managerRequest,
			fullyAvailableCandidates,
			partiallyAvailableCandidates,
			fullyAllocatedCandidates);
		try
		{
			var llmClient = _llmClientFactory.Create(provider);
			var raw = await CallLlmWithRetryAsync(llmClient, prompt, apiKey, cancellationToken);
			var eligibleIds = fullyAvailableCandidates.Select(candidate => candidate.Id).ToHashSet();
			var response = AiResponseParser.ParseTeamBuilderFromPrompt(raw, eligibleIds);

			await LogAiRequestAsync("TEAM_BUILDER", prompt, raw, managerUserId, cancellationToken);

			if (response.Roles.Count == 0 && string.IsNullOrWhiteSpace(response.Summary) && response.GapAnalysis.Count == 0)
			{
				return BuildDeterministicTeamBuilderFromPrompt(
					managerRequest,
					fullyAvailableCandidates,
					partiallyAvailableCandidates,
					fullyAllocatedCandidates);
			}

			response.AiGenerated = true;
			foreach (var role in response.Roles)
			{
				EnrichCandidateNames(new AiSkillMatchResponseDto { Candidates = role.Matches }, fullyAvailableCandidates);
			}

			return response;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "LLM team builder failed for manager {ManagerUserId}", managerUserId);
			return new TeamBuilderResponseDto
			{
				Message = FallbackTeamBuilderMessage,
				AiGenerated = false,
				Disclaimer = string.Empty,
				GapAnalysis = BuildPromptGapAnalysis(managerRequest, allCandidates, fullyAvailableCandidates)
			};
		}
	}

	private async Task<List<AiCandidateContext>> BuildOrganizationCandidatesAsync(
		CancellationToken cancellationToken)
	{
		var candidates = await _resourceProfileRepository.GetActiveOrganizationCandidatesAsync(cancellationToken);
		return await BuildCandidateContextsAsync(candidates, cancellationToken);
	}

	private async Task<List<AiCandidateContext>> BuildCandidateContextsAsync(
		IReadOnlyList<ResourceProfile> resourceProfiles,
		CancellationToken cancellationToken)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var fourWeeksAgo = today.AddDays(-28);
		var profileIds = resourceProfiles.Select(member => member.Id).ToList();

		var recentTags = await _timesheetRepository.GetRecentActivityTagsByResourceProfilesAsync(
			profileIds,
			fourWeeksAgo,
			cancellationToken);

		return resourceProfiles.Select(member =>
		{
			var utilization = UtilizationCalculator.CalculateCurrentUtilization(member.ProjectAllocations, today);
			return new AiCandidateContext
			{
				Id = member.Id,
				FullName = member.User.FullName,
				Department = member.Department,
				ManagerName = member.Manager?.FullName,
				EmploymentStatus = member.EmploymentStatus,
				CurrentUtilization = utilization,
				AvailabilityPercent = Math.Max(0, 100 - utilization),
				Skills = member.ResourceProfileSkills
					.Select(skill => $"{skill.Skill.SkillName} ({skill.ProficiencyLevel})")
					.ToList(),
				RecentActivityTags = recentTags.GetValueOrDefault(member.Id) ?? []
			};
		}).ToList();
	}

	private async Task<ProjectAiContext> BuildProjectAiContextAsync(Project project, CancellationToken cancellationToken)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var lastWeekStart = WeekHelper.GetLastCompletedWeekStart(today);
		var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);

		var activeAllocations = project.ProjectAllocations
			.Where(allocation =>
				allocation.AllocationStatus == "ACTIVE"
				&& allocation.AllocationStartDate <= today
				&& allocation.AllocationEndDate >= today)
			.ToList();

		var lastWeekHours = new List<ProjectHoursAiContext>();
		foreach (var allocation in activeAllocations)
		{
			var expectedHours = allocation.AllocationPercentage / 100m * maxWeeklyHours;
			var loggedHours = await _timesheetRepository.GetLoggedHoursForProjectResourceProfileWeekAsync(
				project.Id,
				allocation.ResourceProfileId,
				lastWeekStart,
				cancellationToken);

			lastWeekHours.Add(new ProjectHoursAiContext
			{
				EmployeeName = allocation.ResourceProfile.User.FullName,
				LoggedHours = loggedHours,
				ExpectedHours = expectedHours
			});
		}

		var riskFlags = ProjectHealthEvaluator.EvaluateRiskFlags(
			project,
			activeAllocations,
			lastWeekHours.Select(hours => (hours.EmployeeName, hours.LoggedHours, hours.ExpectedHours)).ToList());

		return new ProjectAiContext
		{
			ProjectName = project.ProjectName,
			HealthStatus = project.HealthStatus,
			EndDate = project.EndDate,
			Milestones = project.Milestones
				.OrderBy(milestone => milestone.SortOrder)
				.Select(milestone => new ProjectMilestoneAiContext
				{
					Title = milestone.MilestoneTitle,
					DueDate = milestone.DueDate,
					Status = milestone.MilestoneStatus,
					IsOverdue = milestone.DueDate < today && milestone.MilestoneStatus != MilestoneStatuses.Done
				})
				.ToList(),
			LastWeekHours = lastWeekHours,
			RiskFlags = riskFlags.Select(flag => flag.Message).ToList()
		};
	}

	private static bool MeetsRoleRequirements(
		AiCandidateContext candidate,
		TeamRoleRequirementDto role,
		IReadOnlyDictionary<long, string> skillMap)
	{
		if (role.SkillIds.Count == 0)
		{
			return true;
		}

		var minLevel = ProficiencyRank(role.MinProficiency);
		foreach (var skillId in role.SkillIds)
		{
			if (!skillMap.ContainsKey(skillId))
			{
				continue;
			}

			var skillName = skillMap[skillId];
			var match = candidate.Skills.FirstOrDefault(skill =>
				skill.StartsWith(skillName, StringComparison.OrdinalIgnoreCase));

			if (match == null)
			{
				return false;
			}

			var levelText = match[(match.IndexOf('(') + 1)..match.IndexOf(')')];
			if (ProficiencyRank(levelText) < minLevel)
			{
				return false;
			}
		}

		return true;
	}

	private static int ProficiencyRank(string level) => level.Trim().ToLowerInvariant() switch
	{
		"advanced" => 3,
		"intermediate" => 2,
		_ => 1
	};

	private async Task<Dictionary<long, string>> LoadSkillMapAsync(
		IEnumerable<TeamRoleRequirementDto> roles,
		CancellationToken cancellationToken)
	{
		var skillIds = roles.SelectMany(role => role.SkillIds).Distinct().ToList();
		if (skillIds.Count == 0)
		{
			return new Dictionary<long, string>();
		}

		return new Dictionary<long, string>(await _skillRepository.GetNamesByIdsAsync(skillIds, cancellationToken));
	}

	private AiSkillMatchResponseDto BuildDeterministicSkillMatch(string requirement, IReadOnlyList<AiCandidateContext> eligible)
	{
		var keywords = requirement
			.Split([' ', ',', '.', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(word => word.ToLowerInvariant())
			.ToHashSet();

		var ranked = eligible
			.Select(candidate => new
			{
				Candidate = candidate,
				Score = ScoreCandidate(candidate, keywords)
			})
			.Where(item => item.Score > 0)
			.OrderByDescending(item => item.Score)
			.ThenByDescending(item => item.Candidate.AvailabilityPercent)
			.Take(5)
			.Select((item, index) => new AiSkillMatchCandidateDto
			{
				EmployeeId = item.Candidate.Id,
				FullName = item.Candidate.FullName,
				Rank = index + 1,
				MatchScore = item.Score,
				Reason = $"Matched skills/tags for requirement; {item.Candidate.AvailabilityPercent:0}% available.",
				AvailabilityPercent = item.Candidate.AvailabilityPercent
			})
			.ToList();

		return new AiSkillMatchResponseDto
		{
			Candidates = ranked,
			AiGenerated = ranked.Count > 0,
			Summary = ranked.Count > 0
				? $"Found {ranked.Count} organization-wide candidate(s) with available capacity."
				: null,
			Message = ranked.Count == 0 ? "No keyword matches found. Configure LLM API key for better results." : null,
			GapAnalysis = ranked.Count == 0
				? ["No exact organization-wide keyword match was found. Consider training an available employee or hiring for this skill."]
				: []
		};
	}

	private static int ScoreCandidate(AiCandidateContext candidate, HashSet<string> keywords)
	{
		return keywords.Count(keyword => CandidateHasSearchTerm(candidate, keyword)) * 20
		       + (int)candidate.AvailabilityPercent / 10;
	}

	private TeamBuilderResponseDto BuildDeterministicTeamBuilder(
		IReadOnlyList<TeamRoleRequirementDto> roles,
		IReadOnlyDictionary<string, IReadOnlyList<AiCandidateContext>> candidatesByRole)
	{
		var response = new TeamBuilderResponseDto
		{
			AiGenerated = false,
			Message = "LLM API key not configured. Showing rule-based matches."
		};

		foreach (var role in roles)
		{
			var matches = BuildDeterministicMatches(role, candidatesByRole.GetValueOrDefault(role.RoleTitle) ?? []);
			response.Roles.Add(new TeamBuilderRoleResultDto
			{
				RoleTitle = role.RoleTitle,
				Headcount = role.Headcount,
				Matches = matches
			});
		}

		return response;
	}

	private TeamBuilderResponseDto BuildDeterministicTeamBuilderFromPrompt(
		string managerRequest,
		IReadOnlyList<AiCandidateContext> fullyAvailableCandidates,
		IReadOnlyList<AiCandidateContext> partiallyAvailableCandidates,
		IReadOnlyList<AiCandidateContext> fullyAllocatedCandidates)
	{
		var allCandidates = fullyAvailableCandidates
			.Concat(partiallyAvailableCandidates)
			.Concat(fullyAllocatedCandidates)
			.ToList();
		var roleRequests = InferRoleRequests(managerRequest, allCandidates);
		var roles = roleRequests.Select(role =>
		{
			var availableMatches = fullyAvailableCandidates
				.Where(candidate => MatchesPromptRole(candidate, role))
				.OrderByDescending(candidate => ScoreCandidate(candidate, role.SkillTerms.Select(term => term.ToLowerInvariant()).ToHashSet()))
				.ThenBy(candidate => candidate.FullName)
				.Take(role.Headcount)
				.Select((candidate, index) => new AiSkillMatchCandidateDto
				{
					EmployeeId = candidate.Id,
					FullName = candidate.FullName,
					Rank = index + 1,
					MatchScore = 75,
					Reason = BuildRoleSelectionReason(candidate, role, allCandidates),
					AvailabilityPercent = candidate.AvailabilityPercent
				})
				.ToList();

			return new TeamBuilderRoleResultDto
			{
				RoleTitle = role.RoleTitle,
				Headcount = role.Headcount,
				Matches = availableMatches
			};
		}).ToList();
		var matchCount = roles.Sum(role => role.Matches.Count);

		return new TeamBuilderResponseDto
		{
			AiGenerated = false,
			Summary = matchCount > 0
				? $"Recommended {matchCount} fully available resource(s) across {roles.Count} requested role(s)."
				: "No fully available organization-wide candidates matched the requested role(s).",
			Message = "LLM API key not configured or response could not be parsed. Showing rule-based team builder guidance.",
			GapAnalysis = BuildPromptGapAnalysis(
				managerRequest,
				allCandidates,
				fullyAvailableCandidates),
			Roles = roles
		};
	}

	private static List<PromptRoleRequest> InferRoleRequests(
		string managerRequest,
		IReadOnlyList<AiCandidateContext> candidates)
	{
		var request = managerRequest.ToLowerInvariant();
		var roles = new List<PromptRoleRequest>();
		var knownSkills = candidates
			.SelectMany(candidate => candidate.Skills)
			.Select(ExtractSkillName)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var skill in knownSkills)
		{
			if (ContainsSearchTerm(request, skill))
			{
				roles.Add(new PromptRoleRequest($"{skill} Resource", 1, [skill]));
			}
		}

		if ((request.Contains("qa", StringComparison.OrdinalIgnoreCase)
		     || request.Contains("quality", StringComparison.OrdinalIgnoreCase)
		     || request.Contains("test", StringComparison.OrdinalIgnoreCase))
		    && roles.All(role => !role.RoleTitle.Contains("QA", StringComparison.OrdinalIgnoreCase)))
		{
			roles.Add(new PromptRoleRequest("QA Engineer", 1, ["qa", "selenium", "testing"]));
		}

		if (roles.Count == 0)
		{
			roles.Add(new PromptRoleRequest("Suggested team", 1, request
				.Split([' ', ',', '.', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(word => word.Length > 2)
				.ToList()));
		}

		return roles;
	}

	private static bool MatchesPromptRole(AiCandidateContext candidate, PromptRoleRequest role)
	{
		return role.SkillTerms.Count == 0
		       || role.SkillTerms.Any(term => CandidateHasSearchTerm(candidate, term));
	}

	private static string BuildRoleSelectionReason(
		AiCandidateContext selected,
		PromptRoleRequest role,
		IReadOnlyList<AiCandidateContext> allCandidates)
	{
		var unavailableMatches = allCandidates
			.Where(candidate => candidate.Id != selected.Id && candidate.AvailabilityPercent < 100 && MatchesPromptRole(candidate, role))
			.OrderByDescending(candidate => candidate.CurrentUtilization)
			.Take(2)
			.Select(candidate => $"{candidate.FullName} ({candidate.AvailabilityPercent:0}% available)")
			.ToList();
		var comparison = unavailableMatches.Count > 0
			? $" Chosen before unavailable matches: {string.Join(", ", unavailableMatches)}."
			: " Chosen because no stronger unavailable match was required for this role.";

		return $"Best 100% available fit for {role.RoleTitle} based on skills/department signals.{comparison}";
	}

	private static string ExtractSkillName(string skill)
	{
		var bracketIndex = skill.IndexOf('(');
		return bracketIndex > 0 ? skill[..bracketIndex].Trim() : skill.Trim();
	}

	private static bool CandidateHasSearchTerm(AiCandidateContext candidate, string term)
	{
		return candidate.Skills.Select(ExtractSkillName).Any(skill => ContainsSearchTerm(skill, term))
		       || candidate.RecentActivityTags.Any(tag => ContainsSearchTerm(tag, term))
		       || ContainsSearchTerm(candidate.Department ?? string.Empty, term);
	}

	private static bool ContainsSearchTerm(string text, string term)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term))
		{
			return false;
		}

		var pattern = $@"(?<![A-Za-z0-9]){Regex.Escape(term.Trim())}(?![A-Za-z0-9])";
		return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
	}

	private static List<string> BuildPromptGapAnalysis(
		string managerRequest,
		IReadOnlyList<AiCandidateContext> allCandidates,
		IReadOnlyList<AiCandidateContext> fullyAvailableCandidates)
	{
		var keywords = managerRequest
			.Split([' ', ',', '.', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(word => word.ToLowerInvariant())
			.Where(word => word.Length > 2)
			.ToHashSet();
		var matchingCandidates = allCandidates
			.Where(candidate => ScoreCandidate(candidate, keywords) > 0)
			.ToList();
		var gaps = new List<string>();

		if (fullyAvailableCandidates.Count == 0)
		{
			gaps.Add("There are resources in the organization, but no employee is 100% available for team builder right now.");
		}

		var partialMatches = matchingCandidates.Where(candidate => candidate.AvailabilityPercent > 0 && candidate.AvailabilityPercent < 100).ToList();
		if (partialMatches.Count > 0)
		{
			gaps.Add($"{partialMatches.Count} matching resource(s) have partial availability, but team builder only selects 100% available employees.");
		}

		var fullyAllocatedMatches = matchingCandidates.Where(candidate => candidate.AvailabilityPercent <= 0).ToList();
		if (fullyAllocatedMatches.Count > 0)
		{
			gaps.Add($"{fullyAllocatedMatches.Count} matching resource(s) exist but are currently fully allocated.");
		}

		if (matchingCandidates.Count == 0)
		{
			gaps.Add("No matching skill signal was found in the organization. Consider training an available employee or hiring for this skill.");
		}

		return gaps;
	}

	private static List<string> BuildRoleGapAnalysis(
		IReadOnlyList<TeamRoleRequirementDto> roles,
		IReadOnlyList<AiCandidateContext> allCandidates,
		IReadOnlyDictionary<long, string> skillMap)
	{
		var gaps = new List<string>();
		foreach (var role in roles)
		{
			var skillNames = role.SkillIds
				.Where(skillMap.ContainsKey)
				.Select(skillId => skillMap[skillId])
				.ToList();
			var matchingCandidates = allCandidates
				.Where(candidate => skillNames.Count == 0 || skillNames.Any(skill =>
					candidate.Skills.Any(candidateSkill => candidateSkill.StartsWith(skill, StringComparison.OrdinalIgnoreCase))))
				.ToList();

			if (matchingCandidates.Count == 0)
			{
				gaps.Add($"No organization-wide skill match found for {role.RoleTitle}. Consider training or hiring.");
				continue;
			}

			var fullyAvailable = matchingCandidates.Count(candidate => candidate.AvailabilityPercent >= 100);
			if (fullyAvailable < role.Headcount)
			{
				gaps.Add(
					$"{role.RoleTitle} needs {role.Headcount} fully available resource(s), but only {fullyAvailable} are 100% available. " +
					$"{matchingCandidates.Count - fullyAvailable} matching resource(s) have partial or no availability.");
			}
		}

		return gaps;
	}

	private static List<AiSkillMatchCandidateDto> BuildDeterministicMatches(
		TeamRoleRequirementDto role,
		IReadOnlyList<AiCandidateContext> candidates)
	{
		return candidates
			.OrderByDescending(candidate => candidate.AvailabilityPercent)
			.Take(role.Headcount)
			.Select((candidate, index) => new AiSkillMatchCandidateDto
			{
				EmployeeId = candidate.Id,
				FullName = candidate.FullName,
				Rank = index + 1,
				MatchScore = 70,
				Reason = $"Meets role skill requirements with {candidate.AvailabilityPercent:0}% availability.",
				AvailabilityPercent = candidate.AvailabilityPercent
			})
			.ToList();
	}

	private static AiRiskSummaryResponseDto BuildTemplateRiskSummary(ProjectAiContext context)
	{
		var overdue = context.Milestones.Count(milestone => milestone.IsOverdue);
		var lowHours = context.LastWeekHours.Count(hours => hours.ExpectedHours > 0 && hours.LoggedHours < hours.ExpectedHours * 0.6m);
		var paragraph =
			$"Project {context.ProjectName} is currently {context.HealthStatus}. " +
			$"{overdue} milestone(s) are overdue and {lowHours} resource(s) logged significantly fewer hours than expected last week. " +
			"Review milestone dates and speak with affected team members.";

		return new AiRiskSummaryResponseDto
		{
			Paragraph = paragraph,
			AiGenerated = false,
			Message = "LLM API key not configured. Showing template summary.",
			ProjectName = context.ProjectName,
			HealthStatus = context.HealthStatus
		};
	}

	private static void EnrichCandidateNames(AiSkillMatchResponseDto response, IReadOnlyList<AiCandidateContext> candidates)
	{
		var nameMap = candidates.ToDictionary(candidate => candidate.Id, candidate => candidate.FullName);
		foreach (var match in response.Candidates)
		{
			if (nameMap.TryGetValue(match.EmployeeId, out var name))
			{
				match.FullName = name;
				match.AvailabilityPercent = candidates.First(candidate => candidate.Id == match.EmployeeId).AvailabilityPercent;
			}
		}
	}

	private async Task EnforceRateLimitAsync(long managerUserId, CancellationToken cancellationToken)
	{
		var since = DateTime.UtcNow.AddHours(-1);
		var count = await _aiRequestLogRepository.CountByUserSinceAsync(managerUserId, since, cancellationToken);

		if (count >= MaxAiRequestsPerHour)
		{
			throw new ValidationException("AI request limit reached. Please try again later.");
		}
	}

	private async Task LogAiRequestAsync(
		string requestType,
		string prompt,
		string responseSummary,
		long managerUserId,
		CancellationToken cancellationToken)
	{
		await _aiRequestLogRepository.LogAsync(new AiRequestLog
		{
			RequestType = requestType,
			Prompt = Truncate(prompt, 4000),
			ResponseSummary = Truncate(responseSummary, 2000),
			RequestedByUserId = managerUserId,
			CreatedAt = DateTime.UtcNow
		}, cancellationToken);
	}

	private static string Truncate(string value, int maxLength) =>
		value.Length <= maxLength ? value : value[..maxLength];

	private async Task<decimal> GetMaxWeeklyHoursAsync(CancellationToken cancellationToken)
	{
		var value = await _systemConfigRepository.GetValueByKeyAsync(SystemConfigKeys.MaxWeeklyHours, cancellationToken);
		return decimal.TryParse(value, out var maxHours) && maxHours > 0 ? maxHours : DefaultMaxWeeklyHours;
	}

	private static async Task<string> CallLlmWithRetryAsync(
		ILlmClient llmClient,
		string prompt,
		string apiKey,
		CancellationToken cancellationToken)
	{
		var delays = new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };
		Exception? lastError = null;

		foreach (var delay in delays)
		{
			if (delay > TimeSpan.Zero)
			{
				await Task.Delay(delay, cancellationToken);
			}

			try
			{
				return await llmClient.GenerateResponseAsync(prompt, apiKey, cancellationToken);
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
			{
				lastError = ex;
			}
		}

		throw lastError ?? new InvalidOperationException("LLM call failed.");
	}

	private sealed record PromptRoleRequest(string RoleTitle, int Headcount, List<string> SkillTerms);
}
