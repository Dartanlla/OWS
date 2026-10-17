# OWS Public API Lambda

This project exposes selected OWS Public API actions through the API Gateway proxy handler in `Function.cs`. The handler does not automatically inherit routes from the ASP.NET `OWSPublicAPI` controller.

## Connection ticket endpoint

Route API Gateway `POST /api/Users/ExternalConnectionTicket` to this Lambda function. Include the existing `X-CustomerGUID` header and a JSON body:

```json
{
  "UserSessionGUID": "00000000-0000-0000-0000-000000000000",
  "ServerId": 1
}
```

Use a real OWS session GUID and the intended Nilux Edge server ID. The session must have a selected character. The response contains an opaque ticket and a fresh Base64 session secret; both must be treated as credentials. The ticket expires after 45 seconds. The API Gateway endpoint must be HTTPS.

Route `POST /api/Users/RedeemExternalConnectionTicket` to the same Lambda for
Edge-only validation. Edge first sends `Claim: false` to retrieve the secret and
authenticate the encrypted UDP handshake, then sends `Claim: true` with a stable
`ClaimantId` to atomically claim the ticket. Both requests must include the
`X-Edge-Redemption-Key` header.

## Lambda environment variables

In the AWS Lambda function configuration, set:

- `OWS_DB_CONNECTION_STRING`: existing OWS PostgreSQL connection string (the handler also accepts `OWSDBConnectionString` or `OWSStorageConfig__OWSDBConnectionString`).
- `OWSTicketConfig__ConnectionString`: Valkey connection string used by `TicketRepository`, for example `valkey-endpoint:6379,password=REPLACE_ME,ssl=true,abortConnect=false`. Use the actual endpoint, port, authentication, and TLS settings of the AWS Valkey deployment. Do not commit the real connection string to source or `aws-lambda-tools-defaults.json`.
- `OWS_TICKET_REDEMPTION_API_KEY`: high-entropy shared secret accepted only from Nilux Edge redemption clients. Configure the same value on Edge as `NILUX_OWS_REDEMPTION_API_KEY`.

The ticket settings are optional for other Lambda routes. Missing ticket-store
or redemption-key configuration causes the corresponding endpoint to return
HTTP 503. Lambda—not Edge—must have network access to Valkey.
