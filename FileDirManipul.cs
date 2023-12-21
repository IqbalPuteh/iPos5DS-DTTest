using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
public abstract class DirectoryManipulator
{
    public enum FileExtension
    {
        Excel,
        Log,
        Zip
    }

    public virtual string CreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            return "";
        }
        Directory.CreateDirectory(path);
        return path;
    }

    public virtual string DeleteFilesBase(string path, string extension)
    {
        DirectoryInfo directory = new DirectoryInfo(path);
        foreach (FileInfo file in directory.GetFiles($"{extension}"))
        {
            file.Delete();
        }
        foreach (DirectoryInfo subDirectory in directory.GetDirectories())
        {
            DeleteFilesBase(subDirectory.FullName, extension);
        }
        return "";
    }


    public virtual string ZipDirectory(string sourcePath, string destinationPath)
    {

        return "";
    }

    public virtual string SendFileToUrl(string filePath, string url)
    {

        return "";       
    }


}

public class  MyDirectoryManipulator : DirectoryManipulator
{
   

    public override string CreateDirectory(string path)
    {
        var value = base.CreateDirectory(path)  == "" ? "" : $"Creating directory at {path}";
        return value;

    }

    public string DeleteFiles(string path, FileExtension fileExtension)
    {
        string extension = string.Empty;

        switch (fileExtension)
        {
            case FileExtension.Excel:
                extension = "*.xl*";
                break;
            case FileExtension.Log:
                extension = "*.log";
                break;
            case FileExtension.Zip:
                extension = "*.zip";
                break;
        }
        base.DeleteFilesBase(path, extension);
        return ($"Deleting files with extension {extension} in {path}");
    }

    public override string ZipDirectory(string sourcePath, string destinationPath)
    {
        base.ZipDirectory(sourcePath, destinationPath);
        return ($"Zipping directory at {sourcePath} to {destinationPath}");
    }

    public override string SendFileToUrl(string filePath, string url)
    {
        base.SendFileToUrl(filePath, url);
        return ($"Sending file at {filePath} to {url}");

    }


}

public struct DateManipultor
{
    public static string GetPrevMonth()
    {
        return DateTime.Now.AddMonths(-1).ToString("MM");
    }

    public static string GetPrevYear()
    {
        return DateTime.Now.AddMonths(-1).ToString("yyyy");
    }

    public static string GetFirstDate()
    {
        return "01";
    }

    public static string GetLastDayOfPrevMonth()
    {
        var lastDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
        return lastDay.ToString("dd");
    }

    public static string GetDateFrom()
    {
        return $@"{GetFirstDate}/{GetPrevMonth}/{GetPrevYear} 00:00";
    }

    public static string GetDateTo()
    {
        return $@"{GetLastDayOfPrevMonth}/{GetPrevMonth}/{GetPrevYear} 00:00";
    }

    public static string GetDSPeriod()
    {
        return $@"{GetPrevYear}{GetPrevMonth}";
    }

   
}

public class Datemanipulation
{
    private readonly string _getPrevMonth;
    private readonly string _getPrevYear;
    private readonly string _getFirstDateofPrevMonth;
    private readonly string _getLastDateofPrevMonth;
    private readonly string _getDateFrom;
    private readonly string _getDateTo;
    private readonly string _getDSPeriod;

    public string GetPrevMonth => _getPrevMonth;

    public string GetPrevYear => _getPrevYear;

    public string GetFirstDateofPrevMonth => _getFirstDateofPrevMonth;

    public string GetLastDateofPrevMonth => _getLastDateofPrevMonth;

    public string GetDateFrom => _getDateFrom;

    public string GetDateTo => _getDateTo;

    public string GetDSPeriod => _getDSPeriod;

    public Datemanipulation()
    {
        _getPrevMonth = DateTime.Now.AddMonths(-1).ToString("MM");
        _getPrevYear = DateTime.Now.AddMonths(-1).ToString("yyyy");
        _getFirstDateofPrevMonth = DateTime.Now.AddMonths(-1).ToString("MM");
        _getLastDateofPrevMonth = new  DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1).ToString("DD");
        _getDateFrom = $@"{_getFirstDateofPrevMonth}/{_getPrevMonth}/{_getPrevYear} 00:00";
        _getDateTo = $@"{_getLastDateofPrevMonth}/{_getPrevMonth}/{_getPrevYear} 00:00";
        _getDSPeriod = $@"{_getPrevYear}{_getPrevMonth}";
    }

}

public struct mycUrl
{
    const string _cUrlProdSecureHTTP = "http://dashboard.fairbanc.app/api/documents";
    const string _cUrlTestSecureHTTP = "https://sandbox.fairbanc.app/api/documents";
    const string _cUrlProdUnsecureHTTP = "http://dashboard.fairbanc.app/api/documents";
    const string _cUrlTestUnsecureHTTP = "http://sandbox.fairbanc.app/api/documents";
    const string _prodToken = "2S0VtpYzETxDrL6WClmxXXnOcCkNbR5nUCCLak6EHmbPbSSsJiTFTPNZrXKk2S0VtpYzETxDrL6WClmx";
    const string _testToken = "KQtbMk32csiJvm8XDAx2KnRAdbtP3YVAnJpF8R5cb2bcBr8boT3dTvGc23c6fqk2NknbxpdarsdF3M4V";

    private string _httpresponses;

    private static string _token;
    private static string _urlAddress;
    private static string _targetFile;
    //private static HttpClient? _httpClient;

    public string TargetFile => _targetFile;

    public string cUrlProdSecureHTTP => _urlAddress;

    public string Httpresponses => _httpresponses;

    public static string SendRequest(char isSecureHttp = 'Y', char isSandbox = 'Y', string fileandpathname = "")
    {
        _urlAddress = isSecureHttp switch
        {
            'Y' => isSandbox == 'Y' ? _cUrlTestSecureHTTP : _cUrlProdSecureHTTP,
            _ => isSandbox == 'Y' ? _cUrlTestUnsecureHTTP : _cUrlProdUnsecureHTTP
        };

        _token = isSandbox switch
        {
            'Y' => _testToken,
            _ => _prodToken
        };
        _targetFile = fileandpathname;
        using var _httpClient = new HttpClient();
        var multipartFormDataContent = new MultipartFormDataContent();
        multipartFormDataContent.Add(new StringContent(_token), "api_token");
        multipartFormDataContent.Add(new ByteArrayContent(File.ReadAllBytes(_targetFile)), "file", Path.GetFileName(_targetFile));

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _urlAddress);

        var Content = multipartFormDataContent;

        var httpResponseMessage = _httpClient.Send(httpRequestMessage);

        Thread.Sleep(5000);
        httpResponseMessage.EnsureSuccessStatusCode();

        var strResponseBody = httpResponseMessage.Content.ReadAsStream();
        var array = httpResponseMessage.ToString().Split(':', ',');
        var _httpresponses = array[1].Trim();
        return _httpresponses;
    }
}




