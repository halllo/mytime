using System.Net.Http.Json;
using static TimeRecordsClient.TimeRecordsResponse;

public class TimeRecordsClient
{
	private readonly HttpClient http;

	public TimeRecordsClient(HttpClient http)
	{
		this.http = http;
	}

	public enum TimeRange
	{
		Today,
		Yesterday,
		ThisWeek,
		LastWeek,
		ThisMonth,
		LastMonth,
		ThisYear,
		LastYear
	}

	public async Task<IReadOnlyList<TimeRecordDto>> Get(TimeRange range, Guid userId)
	{
		List<TimeRecordDto> timeRecords = new();

		await Page(async (skip, take, totalCount) =>
		{
			var response = await this.http.GetAsync($"performers/{userId}/timeEntries/of/{range.ToString().ToLowerInvariant()}?api-version=1.0&timezone=Europe/Berlin&skip={skip}&take={take}&system=kms");
			response.EnsureSuccessStatusCode();
			var timeRecordsResponse = (await response.Content.ReadFromJsonAsync<TimeRecordsResponse>())!;
			totalCount(timeRecordsResponse.TotalCount);

			timeRecords.AddRange(timeRecordsResponse.PageEntities);
		});

		return timeRecords;
	}

	delegate Task PageHandler(int skip, int take, Action<int> totalCount);
	static async Task Page(PageHandler pageHandler)
	{
		var take = 100;
		var page = 0;
		var totalCount = -1;

		do
		{
			var skip = take * page++;
			await pageHandler(skip, take, c => totalCount = c);

		} while (totalCount > (take * page));
	}

	public class TimeRecordsResponse
	{
		public int Skip { get; set; }
		public int Take { get; set; }
		public int TotalCount { get; set; }
		public TimeRecordDto[] PageEntities { get; set; } = null!;

		public class TimeRecordDto
		{
			public Guid Id { get; set; }
			public DateTimeOffset? From { get; set; }
			public DateTimeOffset? To { get; set; }
			public DateTimeOffset Date { get; set; }
			public string ForeignSystemId { get; set; } = null!;
			public int DurationInSeconds { get; set; }
			public int AggregatedDurationInSeconds { get; set; }
			public string Description { get; set; } = null!;
			public string PerformedBy { get; set; } = null!;
			public DateTimeOffset CreatedAt { get; set; }
			public string CreatedBy { get; set; } = null!;
			public DateTimeOffset? UpdatedAt { get; set; }
			public string UpdatedBy { get; set; } = null!;
			public string UpdatedByName { get; set; } = null!;
			public bool ReadOnly { get; set; }
			public string TenantId { get; set; } = null!;
			public string RowVersion { get; set; } = null!;
			public bool RunningStopwatch { get; set; }
			public int StopwatchGroupCount { get; set; }
			public string Type { get; set; } = null!;
			public string State { get; set; } = null!;
			public string StateId { get; set; } = null!;
			public string TypeId { get; set; } = null!;
			public KmsMatterTimeEntryDto KmsMatterTimeEntry { get; set; } = null!;
			public KmsManagementTimeEntryDto KmsManagementTimeEntry { get; set; } = null!;
			public StopwatchDto[] Stopwatches { get; set; } = null!;

			public class LanguageDto
			{
				public Guid Id { get; set; }
				public Guid? XId { get; set; }
				public int InternalId { get; set; }
				public string Bezeichnung { get; set; } = null!;
				public string Alias { get; set; } = null!;
				public string? CultureDescription { get; set; }
				public string? SystemCultureName { get; set; }
				public bool IsSystem { get; set; }
				public bool IsActive { get; set; }
			}

			public class ActivityTypeDto
			{
				public Guid Id { get; set; }
				public Guid? XId { get; set; }
				public int InternalId { get; set; }
				public string Description { get; set; } = null!;
				public bool BillableFlag { get; set; }
				public string Category { get; set; } = null!;
				public int ContractActivityTypeId { get; set; }
				public int CategoryId { get; set; }
				public bool IsActive { get; set; }
				public bool IsSystem { get; set; }
				public string? Abbreviation { get; set; }
				public TranslationDto[] Translations { get; set; } = null!;
				public string? Code1 { get; set; }
				public string? Code2 { get; set; }

				public class TranslationDto
				{
					public int LanguageId { get; set; }
					public string DisplayName { get; set; } = null!;
					public string? DescriptionTemplate { get; set; }
				}
			}

			public class KmsMatterTimeEntryDto
			{
				public string? InternalId { get; set; }
				public string? Xid { get; set; }
				public int LanguageId { get; set; }
				public int ActivityTypeId { get; set; }
				public string ActivityTypeName { get; set; } = null!;
				public int MatterId { get; set; }
				public string MatterReferenceNumber { get; set; } = null!;
				public string MatterDescription { get; set; } = null!;
				public int? FieldOfLawId { get; set; }
				public string FieldOfLawDescription { get; set; } = null!;
				public int? BillableHoursInSeconds { get; set; }
				public string? InternalComment { get; set; }
				public string? InvoiceNumber { get; set; }
				public LanguageDto Language { get; set; } = null!;
				public ActivityTypeDto ActivityType { get; set; } = null!;
				public MatterDto Matter { get; set; } = null!;
				public FieldOfLawDto FieldOfLaw { get; set; } = null!;

				public class MatterDto
				{
					public Guid Id { get; set; }
					public Guid? XId { get; set; }
					public int InternalId { get; set; }
					public string ReferenceNumber { get; set; } = null!;
					public string Description { get; set; } = null!;
					public string Status { get; set; } = null!;
					public string Subject { get; set; } = null!;
					public string ResponsibleLawyer { get; set; } = null!;
					public int UsingsInTimerecords { get; set; }
					public bool IsLooked { get; set; }
					public object? Amount { get; set; }
					public string Language { get; set; } = null!;
					public string Location { get; set; } = null!;
					public int? MainFieldOfLawId { get; set; }
					public int? LanguageId { get; set; }
					public object? FeeCap { get; set; }
					public bool IsCap { get; set; }
				}

				public class FieldOfLawDto
				{
					public Guid Id { get; set; }
					public Guid? XId { get; set; }
					public int InternalId { get; set; }
					public string Bezeichnung { get; set; } = null!;
				}
			}

			public class KmsManagementTimeEntryDto
			{
				public string? InternalId { get; set; }
				public Guid? Xid { get; set; }
				public int? LanguageId { get; set; }
				public int ActivityTypeId { get; set; }
				public string ActivityTypeName { get; set; } = null!;
				public ActivityTypeDto ActivityType { get; set; } = null!;
				public LanguageDto Language { get; set; } = null!;
			}

			public class StopwatchDto
			{
				public string Id { get; set; } = null!;
				public DateTimeOffset? From { get; set; }
				public DateTimeOffset? To { get; set; }
				public int DurationInSeconds { get; set; }
				public int DurationCalculationType { get; set; }
				public int CalculatedDurationInSeconds { get; set; }
			}
		}
	}
}