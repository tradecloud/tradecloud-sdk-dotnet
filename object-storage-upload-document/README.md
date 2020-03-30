# Upload document

This example uploads a document to the Tradecloud object storage.

## Prerequisites

.NET Core (Runtime or SDK) 3.1 https://dotnet.microsoft.com/download/dotnet-core/3.1

## Clone

```
➜ git clone https://github.com/tradecloud/tradecloud-api-dotnet-sdk.git
Cloning into 'tradecloud-api-dotnet-sdk'...
remote: Enumerating objects: 15, done.
remote: Counting objects: 100% (15/15), done.
remote: Compressing objects: 100% (11/11), done.
remote: Total 15 (delta 2), reused 11 (delta 2), pack-reused 0
Receiving objects: 100% (15/15), done.
Resolving deltas: 100% (2/2), done.
➜  cd tradecloud-api-dotnet-sdk 
➜  tradecloud-api-dotnet-sdk git:(master) cd object-storage-upload-document 
```

## Configure

In the source code:
- username on Tradecloud
- password on Tradecloud
- path (for example `test.pdf`)

## Run

```
➜  object-storage-upload-document git:(master) dotnet run
Tradecloud upload document example.
Authenticate StatusCode: 200
Authenticate Content: OK
Uploading document...please wait
UploadDocument StatusCode: 200
UploadDocument Content: {"id":"bc107c82-76de-4c21-a7c9-b1d2faa75b1e"}
```