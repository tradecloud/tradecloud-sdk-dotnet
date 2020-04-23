# Authentication

This example authenticates a user, retrieves acesss and refresh tokens, refreshes the access token using the refresh token, and logout

The `Authentication` library is re-used in other examples.

## Configure

Enter in the source code:
- username on Tradecloud
- password on Tradecloud

## Run

```
➜  authentication git:(master) ✗ dotnet run
Tradecloud authentication example.
Authenticating..
Authenticate StatusCode: 200
Authenticate Content: {"username":... "status":"authenticated"}
accessToken: ...
refreshToken: ...
Refreshing...
Refresh StatusCode: 200
Refresh Content: {"username":... "status":"authenticated"}
refreshedAccessToken: ...
refreshedRefreshToken: ...
Logout...
Logout StatusCode: 401
Logout Content: The supplied authentication is invalid
loggedOutAccessToken: 
loggedOutRefreshToken: 
Refreshing after logout...
Refresh StatusCode: 401
Refresh Content: The supplied authentication is invalid
refreshedAfterLogoutAccessToken: 
refreshedAfterLogoutRefreshToken: 
```
