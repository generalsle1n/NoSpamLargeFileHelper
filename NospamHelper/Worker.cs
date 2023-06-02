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
        private readonly MailHelper _mailHelper;
        private readonly IConfiguration _config;
        private HttpClient _http = new HttpClient();
        private const int _waitTime = (1 * 1000) * 20;
        public Worker(ILogger<Worker> logger, NoSpamHelper NoSpamHelper, VirustotalHelper virustotalHelper, MailHelper MailHelper, IConfiguration Config)
        {
            _logger = logger;
            _nospamHelper = NoSpamHelper;
            _virusTotalHelper = virustotalHelper;
            _mailHelper = MailHelper;
            _config = Config;
        }
        private async Task ProcessSingleFile(LargeFileEntry LargeFile)
        {
            HttpResponseMessage response = await _http.GetAsync(LargeFile.DownloadUrl);
            byte[] fileContent = await response.Content.ReadAsByteArrayAsync();

            bool Result = await _virusTotalHelper.ProccessFile(fileContent, LargeFile);
            if (Result)
            {
                _logger.LogInformation($"{LargeFile.Name} clean");
                _nospamHelper.ReleaseLargeFile(LargeFile);

                if (_config.GetValue<bool>("MailNotification"))
                {
                    _mailHelper.SendNotification(LargeFile);
                }
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