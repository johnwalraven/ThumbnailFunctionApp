# ThumbnailFunctionApp

* .Net version: 6
* Function Runtime: v4

This function app responds to a new image that is added to a blob container using an EventGridEvent. The function app generates the thumbnail and places it a different container with the same image name. The function app can create thumbnails for png, jpg, jpeg, and gif.

The width of the thumbnail can be set in the deployment via the 'thumbnailWidth' property. This can also be updated at anytime after by changing the Function App Configuration in the Azure Portal.

# Deploy the function code

Below is a bash script that can be used to deploy all the function app without building the app locally.

```bash
#!/bin/bash

# Create a resource group.
az group create --name myResourceGroup --location location

# deploy, specifying all template parameters
az deployment group create \
--resource-group myResourceGroup \
--template-uri 'https://raw.githubusercontent.com/johnwalraven/ThumbnailFunctionApp/master/azuredeploy.json' \
--parameters 'storageAccountName=myStorageAccount' \
             'imageStorageContainerName=images' \
             'functionName=myFunction' \
             'appServiceName=myAppService' \
             'thumbnailStorageContainerName=thumbnails' \
             'thumbnailWidth=200'

# deploy the function app directly from the repo 
az functionapp deployment source config --name myFunctionName --resource-group myResourceGroup --branch master --manual-integration --repo-url https://github.com/johnwalraven/ThumbnailFunctionApp

# deploy the system topic, specifying all template parameters. This cannot be run with the intial deployment as the function has not deployed. This deployment will create an event topic and subscribe to a blob created event that occurs.
az deployment group create \
--resource-group myResourceGroup \
--template-uri 'https://raw.githubusercontent.com/johnwalraven/ThumbnailFunctionApp/master/azuredeploy.systemtopics.json' \
--parameters 'systemTopicName=systopic' \
             'storageAccountName=myStorageAccount' \
             'functionName=myFunction' \
             'eventSubjectFilter=/blobServices/default/containers/images/'
```

Once the deployment has succeeded, upload an image to the image container and check the thumbnail container for your image.

# Run locally.

To run this function app locally, you will need to install ngrok and set event grid to use a webhook. There is a tutorial on how to set this up from Microsoft, it can be found [here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local).