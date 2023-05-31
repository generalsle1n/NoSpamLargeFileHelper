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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                List<LargeFileEntry> AllFiles = _nospamHelper.GetUnprocessedLargeFiles();
                List<Task<ScanResult>> AllScans = new List<Task<ScanResult>>();
                foreach (LargeFileEntry file in AllFiles)
                { 
                    ProcessSingleFile(file);
                }

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }
    }
}