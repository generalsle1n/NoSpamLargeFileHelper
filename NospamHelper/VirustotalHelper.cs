using NospamHelper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirusTotalNet;
using VirusTotalNet.ResponseCodes;
using VirusTotalNet.Results;

namespace NospamHelper
{
    public class VirustotalHelper
    {
        VirusTotal _virusTotal;
        private readonly ILogger<VirustotalHelper> _logger;
        private readonly IConfiguration _configuration;
        private readonly NoSpamHelper _nospamHelper;
        private const int _waitTime = 60 * 1000;
        private const int _loopMaxUpload = 5;

        public VirustotalHelper(ILogger<VirustotalHelper> logger, IConfiguration configuration, NoSpamHelper NospamHelper)
        { 
            _logger = logger; 
            _configuration = configuration;
            _nospamHelper = NospamHelper;

            InitApi();
        }

        private void InitApi()
        {
            _virusTotal = new VirusTotal(_configuration.GetValue<string>("VirusTotalApi"));
            _virusTotal.UseTLS = true;
            //_virusTotal.Timeout = new TimeSpan(0,5,0);
        }

        internal async Task<ResultFromScan> UploadFile(byte[] Data, string FileName)
        {
            ScanResult Result = await _virusTotal.ScanFileAsync(Data, FileName);
            _logger.LogInformation($"Uploaded File {FileName}");
            int count = 0;
            while(count <= _loopMaxUpload)
            {
                FileReport report = await GetFileReport(Data);
                ResultFromScan result = DecideOperationOnFileReport(report);

                if(result == ResultFromScan.Clean)
                {
                    return result;
                }
                else
                {
                    _logger.LogInformation($"Wait for Result {_waitTime}");
                    await Task.Delay(_waitTime);
                }
            }
            return ResultFromScan.NotClean;
        }

        internal async Task<FileReport> GetFileReport(byte[] Data)
        {
            FileReport Result = await _virusTotal.GetFileReportAsync(Data);
            return Result;
        }


        private ResultFromScan DecideOperationOnFileReport(FileReport Result)
        {
            if (Result.ResponseCode == FileReportResponseCode.Present && Result.Positives == 0)
            {
                _logger.LogInformation($"{Result.SHA256} is Clean");
                return ResultFromScan.Clean;
            }
            else if(Result.ResponseCode == FileReportResponseCode.NotPresent || Result.ResponseCode == FileReportResponseCode.Queued)
            {
                _logger.LogInformation($"{Result.Resource} needs to be scanned");
                return ResultFromScan.NotScanned;
            }else
            {
                _logger.LogWarning($"{Result.SHA256} is not Clean");
                return ResultFromScan.NotClean;
            }
        }

        internal async Task<bool> ProccessFile(byte[] Data, LargeFileEntry File)
        {
            FileReport fileReport = await GetFileReport(Data);
            ResultFromScan res = DecideOperationOnFileReport(fileReport);
            if(res == ResultFromScan.Clean)
            {
                return true;
            }
            else if(res == ResultFromScan.NotScanned)
            {
                if (await UploadFile(Data, File.Name) == ResultFromScan.Clean)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

    enum ResultFromScan
    {
        Clean,
        NotClean,
        NotScanned
    }
}
