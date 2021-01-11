# Find user based on id

This example finds a user based on id in the user-search service (the search service)

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- user email

## Run

``` json
➜  FindUserById git:(master) ✗ dotnet run
Tradecloud find user by email example.
Login response StatusCode: 200 ElapsedMilliseconds: 944
Login response Content: ...
FindUserById StatusCode: 200
FindUserById Content: {
  "id": "...",
  "email": "...",
  "roles": [
    "buyer"
  ],
  "companyId": "...",
  "status": "active",
  "profile": {
    "firstName": "...",
    "lastName": "...",
    "position": "",
    "phoneNumber": "",
    "linkedInProfile": ""
  },
  "settings": {
    "notificationInterval": "tenminutes"
  },
  "createdAt": "2020-09-08T15:24:51Z",
  "meta": {
    "messageId": "d24ae21e-dcbf-4b45-8d20-d1400481471d",
    "source": {
      "traceId": "80f63ed1-767a-438e-a0a0-d83fbf745a52",
      "userId": "...",
      "companyId": "..."
    },
    "createdDateTime": "2020-09-09T08:43:52Z"
  }
}
```
