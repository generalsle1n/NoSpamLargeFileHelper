using NospamHelper.Model;
using Quartz;
using VirusTotalNet.ResponseCodes;
using VirusTotalNet.Results;

namespace NospamHelper
{
    public class Worker : IJob
    {
        private readonly ILogger<Worker> _logger;
        private readonly NoSpamHelper _nospamHelper;
        private readonly VirustotalHelper _virusTotalHelper;
        private HttpClient _http = new HttpClient();
        private const int _waitTime = (1 * 1000) * 20;
        public Worker(ILogger<Worker> logger, NoSpamHelper NoSpamHelper, VirustotalHelper virustotalHelper)
        {
            _logger = logger;
            _nospamHelper = NoSpamHelper;
            _virusTotalHelper = virustotalHelper;
        }
        private async Task ProcessSingleFile(LargeFileEntry File)
        {
            HttpResponseMessage response = await _http.GetAsync(File.DownloadUrl);
            byte[] fileContent = await response.Content.ReadAsByteArrayAsync();
            bool Result = await _virusTotalHelper.ProccessFile(fileContent, File.Name);
            if (Result)
            {
                _nospamHelper.ReleaseLargeFile(File);
            }
        }
        public async Task Execute(IJobExecutionContext context)
            {
                List<LargeFileEntry> AllFiles = _nospamHelper.GetUnprocessedLargeFiles();
            foreach(LargeFileEntry File in AllFiles)
                { 
                _logger.LogInformation($"Added File to Queue {File.Name}");
                await ProcessSingleFile(File);
                }

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }
    }
}