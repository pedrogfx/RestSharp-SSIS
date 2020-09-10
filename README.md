# RestSharp-ETL

Data extraction from ZenDesk API with RestSharp library using SSIS Script Component in C #.

Developed using the SQL Server Integration Services (SSIS) tool using the ETL process.

**NuGet package**
>Install-Package Newtonsoft.Json -Version 12.0.3

>Install-Package RestSharp -Version 106.11


In all component scripts, we use the "Inputs and Outputs" part to create the output of the extraction, we create the output as "OutputAPI", and within this category we add the fields that we want to extract with their data type.

* OutputAPI:
  * Output Columns:
    * id : eight-byte signed integer [DT_I8];
    * created_at: database timestamp [DT_DBTIMESTAMP];
    * updated_at: database timestamp [DT_DBTIMESTAMP];
    * type: string [DT_STR];
![alt text](https://github.com/pedrogfx/Extracao-RestSharp/blob/master/ZENDESK/TICKETS/PNG/Print%20output%20example.png)
    
Example that how would be the structure of the extraction of Tickets.

**Within the script we use parameters for some variables, for security reasons, we have a package that has all variables that have any sensitivity (Access to DB's and Tokens).**

These variables are passed from SSIS into the Script as "Read only Variables".

After the extraction is done, we follow the flow with a "Derived Columns", because it is in this component that we can do any treatment on the Data. With that we can direct to the DB with the "OLE DB Destination", mapping the fields that we created and extracted from the API with those of the DB target table.
		
![alt text](https://github.com/pedrogfx/Extracao-RestSharp/blob/master/ZENDESK/TICKETS/PNG/Print%20destination%20example.png)
