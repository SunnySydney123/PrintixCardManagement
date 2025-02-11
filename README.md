Azure Function in .NET (csharp). 
When printix will send a WebHook on a New user Create event, this function will 
a) get the userID 
b) Authorize to Printix API 
c) get users deails like name and email 
d) connect to Azure SQL instance and lookup email to retrive the users Card Number 
e) convert card number to base 64 
f) Update Printix user with the card number in base 64.

The Function is writtten in VS 2022, the Microsoft recommend IDE for Azure Function in .NET
Add  "Azure Devlopment Workloads" in VS2022.  
Printix Client ID, Printix Client Secret, and connection string , are saved as Enviornmental variables in Function Configuration.
