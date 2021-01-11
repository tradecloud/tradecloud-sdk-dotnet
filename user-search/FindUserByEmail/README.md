# Find user based on email

This example finds a user based on email address

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- user email

## Run

```
➜  FindUserByEmail git:(master) ✗ dotnet run
Tradecloud get user by email example.
Login response StatusCode: 200 ElapsedMilliseconds: 944
Login response Content: ...
FindUserByEmail StatusCode: 200
FindUserByEmail Content: {
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