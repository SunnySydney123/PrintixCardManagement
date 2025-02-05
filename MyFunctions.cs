using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace PrintixCardManagement
{
    public class MyFunctions
    {
        private readonly ILogger<MyFunctions> _logger;

        public MyFunctions(ILogger<MyFunctions> logger)
        {
            _logger = logger;
        }

        [Function("PrintixFunction")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("Webhook received a request.");

                // Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"Received data: {requestBody}");

                if (string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogWarning("Request body is empty.");
                    return new BadRequestObjectResult("Request body cannot be empty.");
                }

                // Parse JSON and extract userId
                JObject jsonObject;
                try
                {
                    jsonObject = JObject.Parse(requestBody);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError($"Failed to parse JSON: {parseEx.Message}");
                    return new BadRequestObjectResult("Invalid JSON format.");
                }

                var hrefToken = jsonObject.SelectToken("events[0].href");
                if (hrefToken == null)
                {
                    _logger.LogWarning("Missing 'events[0].href' in the request body.");
                    return new BadRequestObjectResult("Invalid request body. 'events[0].href' is required.");
                }

                string href = hrefToken.ToString();
                if (string.IsNullOrEmpty(href) || !href.Contains("users/"))
                {
                    _logger.LogWarning("Invalid 'href' format.");
                    return new BadRequestObjectResult("Invalid 'href' format in the request body.");
                }

                string userId = href.Substring(href.IndexOf("users/") + "users/".Length);
                _logger.LogInformation($"Extracted userId: {userId}");

                // Get API authorization token from Printix
                var client = new HttpClient();
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://auth.printix.net/oauth/token");
                var collection = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_secret", Environment.GetEnvironmentVariable("PrintixClientSecret")),
                    new KeyValuePair<string, string>("client_id", Environment.GetEnvironmentVariable("PrintixClientId"))
                };
                tokenRequest.Content = new FormUrlEncodedContent(collection);

                HttpResponseMessage tokenResponse;
                try
                {
                    tokenResponse = await client.SendAsync(tokenRequest);
                    tokenResponse.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to retrieve access token: {ex.Message}");
                    return new StatusCodeResult(500);
                }

                var authResponse = await tokenResponse.Content.ReadAsStringAsync();
                JObject authJson;
                try
                {
                    authJson = JObject.Parse(authResponse);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError($"Failed to parse token response: {parseEx.Message}");
                    return new StatusCodeResult(500);
                }

                string accessToken = authJson["access_token"]?.ToString();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Access token is missing in the response.");
                    return new StatusCodeResult(500);
                }

                _logger.LogInformation($"Obtained access token: {accessToken}");

                // Get tenantId from environment variables
                string tenantId = Environment.GetEnvironmentVariable("PrintixTenantId");
                if (string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogError("Tenant ID is not configured in environment variables.");
                    return new StatusCodeResult(500);
                }

                // Make API call to get user details
                var userRequest = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.printix.net/cloudprint/tenants/{tenantId}/users/{userId}");
                userRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

                HttpResponseMessage userResponse;
                try
                {
                    userResponse = await client.SendAsync(userRequest);
                    userResponse.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to retrieve user details: {ex.Message}");
                    return new StatusCodeResult(500);
                }

                var userDetails = await userResponse.Content.ReadAsStringAsync();
                JObject userJson;
                try
                {
                    userJson = JObject.Parse(userDetails);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError($"Failed to parse user details: {parseEx.Message}");
                    return new StatusCodeResult(500);
                }

                string userEmail = userJson["user"]?["email"]?.ToString();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogError("User email is missing in the response.");
                    return new StatusCodeResult(500);
                }

                _logger.LogInformation($"User email: {userEmail}");

                // SQl Connection configuration from enviornment variables and SQL Query

                string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    await sqlConnection.OpenAsync();
                    _logger.LogInformation("SQL connection opened successfully.");


                    string query = "SELECT cardnumber FROM card_details WHERE email = @Email";

                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                    {
                        sqlCommand.Parameters.AddWithValue("@Email", userEmail);

                        using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // _logger.LogInformation($"User from database: {reader["cardnumber"]}");
                                // Retrieve the cardnumber from the database
                                string cardNumber = reader["cardnumber"].ToString();
                                _logger.LogInformation($"Retrieved cardnumber: {cardNumber}");

                                // Convert the cardnumber to Base64
                                string cardNumberBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cardNumber));
                                _logger.LogInformation($"Cardnumber in Base64: {cardNumberBase64}");
                            }
                        }
                    }
                }

                // Return success response with the userId and userEmail
                return new OkObjectResult(new { userId, userEmail, message = "User details retrieved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing request: {ex}");
                return new StatusCodeResult(500);
            }
        }
    }
}