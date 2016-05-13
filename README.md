##Description
Api App handling connection with DocumentDB this will handle communcation with REST API and used with Logic Apps.
Both the AuthKey (primary API key) and the Endpoint URL for the Document DB instance is retrieved in the template.
``` Git
{
	"Name": "endpoint",
	"Value": "[reference(concat('Microsoft.DocumentDb/databaseAccounts/', parameters('databaseAccountName'))).documentEndpoint]"
	}, 
	{
	"Name": "authKey",
	"Value": "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', parameters('databaseAccountName')), '2015-04-08').primaryMasterKey]"
}
```
| Parameter      	| Description                                               | Type | Validation|
| ----------------------|-----------------------------------------------------------|------|-----------|
|Database	 	|Name of the database to connect to			    |String|Required   |
|DatabaseAccountName	|Name of the database account				    |String|Required   |
|AppSvcPlanName	 	|Name of the App Service Plan				    |String|Required   |
|ApiAppName	 	|Name of the Api App					    |String|Required   |

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FLogicAppComponents%2FDocumentDbApiApp%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>    This deploys via Azure Portal (WEB APP)

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FLogicAppComponents%2FDocumentDbApiApp%2Fmaster%2FazuredeployApiApp.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>    This deploys via Azure Portal (API APP)

<!--|endpoint	 |Endpoint URL for the Document DB instance		     |String|Required   |
|authKey	 |Primary or secondary API key to the Document DB instance   |String|Required   |-->
<!--[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/?repository=https://github.com/LogicAppComponents/DocumentDbApiApp/blob/master/azuredeploy.json)
This deploys via Azuredeploy.net GUI -->

<a href="http://armviz.io/#/?load=https://raw.githubusercontent.com/LogicAppComponents/DocumentDbApiApp/master/azuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>      Visualizes components (WEB APP)

<a href="http://armviz.io/#/?load=https://raw.githubusercontent.com/LogicAppComponents/DocumentDbApiApp/master/azuredeployApiApp.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>      Visualizes components (API APP)

## Remarks ##
If the required parameters are not set the web app will fail to load, this due to a initial connection is made on startup.
