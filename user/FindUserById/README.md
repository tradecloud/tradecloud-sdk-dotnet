# Find user based on id

This example finds a user based on id in the user service (the transaction service)

## Configure

In the source code:

- copy `.env.template` to `.env` and fill in `ACCESS_TOKEN`
- user id

## Run

``` json
➜  FindUserById git:(master) ✗ dotnet run
Tradecloud find user by id example.
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
