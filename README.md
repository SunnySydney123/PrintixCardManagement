# Azure Function for Printix WebHook Integration (.NET - C#)

Azure Function in **.NET, (C#)**  

When Printix sends a WebHook on a **New User Create** event, this function will:  

1. Get the `userID`  
2. Authorize to **Printix API**  
3. Get user details like **name** and **email**  
4. Connect to **Azure SQL instance** and look up **email** to retrieve the **user's Card Number**  
5. Convert **card number** to **Base64**  
6. Update **Printix user** with the **card number** in Base64  

## Development Setup  

- The function is written in **Visual Studio 2022**, the **Microsoft-recommended IDE** for **Azure Functions in .NET**  
- Add **Azure Development Workloads** in **Visual Studio 2022**  

## Environment Variables  

- **PRINTIX_CLIENT_ID**  
- **PRINTIX_CLIENT_SECRET**  
- **CONNECTION_STRING**  

These are saved as **environment variables** in **Azure Function Configuration**  

