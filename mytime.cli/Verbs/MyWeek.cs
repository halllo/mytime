using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using popilot;
using Spectre.Console;
using static popilot.CapacityAndWork;

namespace mytime.cli.Verbs
{
	[Verb("get-my-week")]
	class MyWeek
	{
		public async Task Do(ILogger<MyWeek> logger, Me my, TimeRecordsClient timeRecordsClient, AzureDevOps azureDevOps, IConfiguration config)
		{
			var capacityAndWork = new CapacityAndWork(azureDevOps);
			var sources = config.GetSection("Sources").Get<Source[]>() ?? [];
			var universalSources = await sources
				.ToAsyncEnumerable()
				.SelectAwait(async source =>
				{
					if (source.IsAzureDevOpsProject)
					{
						var currentSprint = await capacityAndWork.OfCurrentSprint(
							project: source.Project,
							team: source.Team,
							workItemFilter: i => "Task&Bug".Contains(i.Type),
							workerDetector: WorkerDetector.ChangedByAssignedTo);

						return new
						{
							name = currentSprint.Path,
							start = DateOnly.FromDateTime(currentSprint.Start),
							end = DateOnly.FromDateTime(currentSprint.End),
							days = currentSprint.Days.Select(d => DateOnly.FromDateTime(d)).ToArray(),
							myWork = currentSprint.TeamMembers.Where(t => t.DisplayName == config["DefaultUser"]).FirstOrDefault(),
							myTimeRecords = (IReadOnlyList<TimeRecordsClient.TimeRecordsResponse.TimeRecordDto>?)null,
							myTimeRecordsCapacity = default(decimal?),
						};
					}
					else if (source.IsTimeTracking)
					{
						var thisWeek = (await timeRecordsClient.Get(TimeRecordsClient.TimeRange.ThisWeek, await my.Id())).OrderBy(t => t.Date);
						var lastWeek = (await timeRecordsClient.Get(TimeRecordsClient.TimeRange.LastWeek, await my.Id())).OrderBy(t => t.Date);
						var timeRecords = Enumerable.Concat(lastWeek, thisWeek).ToArray();
						
						return new
						{
							name = source.TimeRecords ?? "",
							start = DateOnly.FromDateTime(timeRecords.First().Date.DateTime),
							end = DateOnly.FromDateTime(timeRecords.Last().Date.DateTime),
							days = Array.Empty<DateOnly>(),
							myWork = default(SprintCapacityAndWork.TeamMember),
							myTimeRecords = (IReadOnlyList<TimeRecordsClient.TimeRecordsResponse.TimeRecordDto>?)timeRecords,
							myTimeRecordsCapacity = source.Capacity,
						};
					}
					else throw new ArgumentOutOfRangeException();
				})
				.ToListAsync();

			var days = universalSources.Where(s => s.myWork != null).SelectMany(s => s.days).Distinct().OrderBy(d => d).ToArray();

			if (!days.Any())
			{
				throw new ArgumentException("No days recognized.");
			}

			Console.WriteLine($"{days.First()}-{days.Last()})");

			var table = new Table();
			table.ShowRowSeparators();
			table.AddColumn("[gray]\nΔCapacity ΔCompletedWork ΔRemainingWork[/]");
			table.BorderColor(Color.Grey);

			foreach (var day in days)
			{
				if (day == DateOnly.FromDateTime(DateTime.Today))
				{
					table.AddColumn(new TableColumn(new Markup($"[bold]{day:ddd}\n{day:dd.MM}[/]")));
				}
				else
				{
					table.AddColumn($"{day:ddd}\n{day:dd.MM}");
				}
			}

			foreach (var universalSource in universalSources)
			{
				if (universalSource.myWork != null)
				{
					var myWork = universalSource.myWork;
					table.AddRow([
						new Markup($"{universalSource.name} ({myWork.CapacityUntilToday} {(myWork.CompletedWorkDeltaUntilToday - myWork.CapacityUntilToday).Against(0.0)} {(myWork.CapacityUntilToday + myWork.RemainingWorkDeltaUntilToday).AgainstInverse(0.0)})"),
						..days
							.Select(day => myWork.Days.FirstOrDefault(wd => DateOnly.FromDateTime(wd.SprintDay) == day))
							.Select(day => day switch
							{
								null => new Markup(" "),
								_ => new Markup($"{(day.IsDayOff ? " " : day.Capacity)} {day.CompletedWorkDelta.Against(day.Capacity)} {day.RemainingWorkDelta.AgainstInverse(day.Capacity)}"),
							})
							.ToArray()
					]);
				}
				else if (universalSource.myTimeRecords != null)
				{
					var myTimeRecords = universalSource.myTimeRecords;
					var myDays = days.Select(day => myTimeRecords.Where(tr => DateOnly.FromDateTime(tr.Date.DateTime) == day).ToArray()).ToArray();
					var dayCount = myDays.Where(d => d.Any()).Count();
					var actualTime = myDays.SelectMany(d => d).Sum(d => TimeSpan.FromSeconds(d.DurationInSeconds).TotalHours);
					var expectedTime = dayCount * (double)(universalSource.myTimeRecordsCapacity ?? 0);
					table.AddRow([
						new Markup($"{universalSource.name} ({expectedTime} {(actualTime - expectedTime).Against(0)})"),
						..myDays
							.Select(day => day switch
							{
								[] => new Markup(" "),
								_ => new Markup($"{day.Sum(d => TimeSpan.FromSeconds(d.DurationInSeconds).TotalHours).Against(universalSource.myTimeRecordsCapacity)}"),
							})
							.ToArray()
					]);
				}
				else throw new ArgumentOutOfRangeException();
			}

			AnsiConsole.Write(table);
		}
	}
	static class CapacitiedFormatting
	{
		public static string Against(this double actual, decimal? capacity) => Against((double?)actual, (double?)capacity);
		public static string Against(this double? actual, double? capacity)
		{
			if (actual == null) return string.Empty;
			else if (actual >= (capacity ?? 0)) return $"[green]{actual}[/]";
			else if (actual < (capacity ?? 0)) return $"[red]{actual}[/]";
			else return string.Empty;
		}

		public static string AgainstInverse(this double? actual, double? capacity)
		{
			if (actual == null) return string.Empty;
			else if (actual * -1 >= (capacity ?? 0)) return $"[green]{actual}[/]";
			else if (actual * -1 < (capacity ?? 0)) return $"[red]{actual}[/]";
			else return string.Empty;
		}
	}

	class Source
	{
		public string? Project { get; set; }
		public string? Team { get; set; }
		public string? TimeRecords { get; set; }
		public decimal? Capacity { get; set; }

		public bool IsAzureDevOpsProject => Project != null && Team != null;
		public bool IsTimeTracking => TimeRecords != null && Capacity != null;
	}
}
