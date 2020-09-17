# Description

Azure Function App used to integrate Ferguson.com online order fulfillment with data provided from UPS API's, such as estimated shipping costs and package tracking information.


# Functions

All functions use UPS API Services. [Documentation can be found here.](https://www.ups.com/upsdeveloperkit?loc=en_US).


### Rating

HTTP endpoint that utilizes UPS Rating Web Service API. Accepts an origin address, destination address, weight and rate type (ground, next day air or second day air) and returns the estimated shipping cost.

[Click here for Open API Definition](https://fergusonupsintegration.azurewebsites.net/api/swagger/ui?code=Y6ldUiv/ljdjVxg7CAdwKNVJjeDHwZLp3tMWDT9AYT00aOxKKJvWhg==)

*To Run Locally:* 

After cloning the repository, add the UPS Web Service to the project. Go to the documentation link above (after signing into your UPS account), and download the _Rating SDK_. 

From the Solution Explorer, navigate to FergusonUPSIntegration project > Connected Services > add Microsoft WCF Web Service Reference Provider > Browse > select the `RateWS.wsdl` file located in the UPS Rating SDK at `RatingPACKAGE\PACKAGEWebServices\SCHEMA-WSDLs`.

Lastly, environment variables will need to be created for UPS account credentials and access numbers.


### Tracking

`AddNewTrackingNumbers` is an Azure Blob Storage Trigger that utilizes the UPS Tracking REST API. When a new CSV file containing UPS tracking numbers is added to the `ups-tracking-numbers` blob container in the `fergusonupsintegrationst` storage account, the `AddNewTrackingNumbers` function makes a 
call to UPS to get the destination address, origin addresses, current status (ready for UPS, in transit, delivered), and current location (city, state) of the package. This data is then written to Azure SQL tables `UPSIntegration.tracking.TrackingNumbers` and `UPSIntegration.tracking.StatusUpdates`

`UpdateTrackingNumbersInTransit` is an Azure Timer Trigger that queries the `UPSIntegration.tracking.StatusUpdates` table and selects any tracking numbers that are in transit (i.e., not delivered, returned or lost). The Tracking API is called for each in-transit tracking number.
 If a status update is availble, a new line is written to the StatusUpdates table containing the current location, status and timestamp.
