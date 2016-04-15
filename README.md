##Description
Api App handling connection with DocumentDB this will handle communcation with REST API and used with Logic Apps.


| Parameter      | Description                                               | Type | Validation|
| ---------------|-----------------------------------------------------------|------|-----------|
|endpoint	 |Endpoint URL for the Document DB instance		     |String|Required   |
|authKey	 |Primary or secudnary API key to the Document DB instnace   |String|Required   |
|database	 |Name of the databse to connect to			     |String|Required   |

## Remarks ##
If the required parameters are not set the web app will fail to load, this due to a initial connection is made on startup.