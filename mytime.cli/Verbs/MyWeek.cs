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
			var currentSprint = await capacityAndWork.OfCurrentSprint(
				project: null,
				team: null,
				workItemFilter: i => "Task&Bug".Contains(i.Type),
				workerDetector: WorkerDetector.AssignedTo);

			Console.WriteLine($"{currentSprint.Path} ({currentSprint.Start}-{currentSprint.End})");

			var table = new Table();
			table.ShowRowSeparators();
			table.AddColumn("[gray]\nΔCapacity ΔCompletedWork ΔRemainingWork[/]");
			table.BorderColor(Color.Grey);

			foreach (var sprintDay in currentSprint.Days)
			{
				if (sprintDay == DateTime.Today)
				{
					table.AddColumn(new TableColumn(new Markup($"[bold]{sprintDay:ddd}\n{sprintDay:dd.MM}[/]")));
				}
				else
				{
					table.AddColumn($"{sprintDay:ddd}\n{sprintDay:dd.MM}");
				}
			}

			foreach (var teamMember in currentSprint.TeamMembers.Where(t => t.DisplayName == config["DefaultUser"]))
			{
				table.AddRow([
					new Markup($"{teamMember.DisplayName} ({teamMember.CapacityUntilToday} {(teamMember.CompletedWorkDeltaUntilToday - teamMember.CapacityUntilToday).Against(0.0)} {(teamMember.CapacityUntilToday + teamMember.RemainingWorkDeltaUntilToday).AgainstInverse(0.0)})"),
					..teamMember.Days
						.Select(day => new Markup($"{(day.IsDayOff ? " " : day.Capacity)} {day.CompletedWorkDelta.Against(day.Capacity)} {day.RemainingWorkDelta.AgainstInverse(day.Capacity)}"))
						.ToArray()
				]);
			}

			AnsiConsole.Write(table);
		}
	}

	static class CapacitiedFormatting
	{
		public static string Against(this double? workDelta, double? capacity)
		{
			if (workDelta == null) return string.Empty;
			else if (workDelta >= (capacity ?? 0)) return $"[green]{workDelta}[/]";
			else if (workDelta < (capacity ?? 0)) return $"[red]{workDelta}[/]";
			else return string.Empty;
		}

		public static string AgainstInverse(this double? workDelta, double? capacity)
		{
			if (workDelta == null) return string.Empty;
			else if (workDelta * -1 >= (capacity ?? 0)) return $"[green]{workDelta}[/]";
			else if (workDelta * -1 < (capacity ?? 0)) return $"[red]{workDelta}[/]";
			else return string.Empty;
		}
	}
}
