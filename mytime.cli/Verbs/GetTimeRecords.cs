using CommandLine;
using Microsoft.Extensions.Logging;

namespace mytime.cli.Verbs
{
	[Verb("get-timerecords")]
	class GetTimeRecords
	{
		public async Task Do(ILogger<GetTimeRecords> logger, Me my, TimeRecordsClient timeRecordsClient)
		{
			var timeRecords = await timeRecordsClient.Get(TimeRecordsClient.TimeRange.ThisWeek, await my.Id());

			foreach (var day in timeRecords.GroupBy(t => DateOnly.FromDateTime(t.Date.Date)))
			{
				Console.WriteLine(day.Key);
				foreach (var matter in day.GroupBy(t => new { ReferenceNumber = t.KmsMatterTimeEntry?.MatterReferenceNumber ?? "<Management>", t.KmsMatterTimeEntry?.MatterDescription }))
				{
					Console.WriteLine($"\t{matter.Key.ReferenceNumber} {matter.Key.MatterDescription}");
					foreach (var time in matter)
					{
						Console.WriteLine($"\t\t{TimeSpan.FromSeconds(time.DurationInSeconds).TotalHours} hours");
					}
				}
			}
		}
	}
}
