# C# Example: Downloading the last completed extraction files by a schedule name

DataScope Select is a hosted delivery platform for non-streaming cross-asset data. It is also the same platform that supports LSEG Tick History. Users can extract data from DSS or Tick History via the DSS Web GUI or REST API.

Data can be extracted through either of two basic mechanisms. The first one is Scheduled Extractions, which occur at a pre-defined moment in time which could be once-of, or recurring. Scheduled extractions can be created using the website, or REST API. The second type of extraction mechanism is OnDemand extractions, which occur immediately when requested. On-Demand extractions can only be created using the REST API. The choice of mechanisms depends on the workflow you want to create.

Typically, users can use the DSS web GUI to download extraction files created by scheduled extractions via the DSS web GUI. This C# example shows the steps to use the DSS REST API to download the last extraction files created by scheduled extractions.

For more information, please refer to [DSS REST API: Downloading Completed Extraction Files by a Schedule Name](https://developers.lseg.com/en/article-catalog/article/dss-rest-api--downloading-completed-extraction-files-by-a-schedu).

## Usage

```
Usage:
        -u, --username... DSS Username

        -p, --password... DSS Password

        -s, --schedulename... A schedule name

        -f, --file[optional]... Type of files (all, note, ric, data)

        -x, --aws[optional]... Set whether show or not
```

## Sample Usages

**1. List all available scheduled extractions**

```
DownloadScheduledExtractedFiles.exe -u <dss_username> -p <dss_password> -s ""
```

**2. Download all files of the last completed extraction by a schedule name**

```
DownloadScheduledExtractedFiles.exe -u <dss_username> -p <dss_password> -s <schedule name>
```
**3. Download a note file of the last completed extraction by a schedule name**

```
DownloadScheduledExtractedFiles.exe -u <dss_username> -p <dss_password> -s <schedule name> -f note
```

**4. Download a data file of the last completed extraction by a schedule name from AWS**

```
DownloadScheduledExtractedFiles.exe -u <dss_username> -p <dss_password> -s <schedule name> -f data -x
```
