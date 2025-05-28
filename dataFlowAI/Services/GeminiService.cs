using System.Text.Json;

namespace dataFlowAI.Services
{
    public class GeminiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;

        // Use the latest supported Gemini model for content generation
        private const string Model = "models/gemini-1.5-flash";
        private const string ApiVersion = "v1beta";

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API key not found");
            _logger = logger;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
            };
        }

        public async Task<string> GenerateDFD(string systemDescription)
        {
            var prompt = $@"Given the following system description, generate a Level 0 Data Flow Diagram in Mermaid.js format.
            Focus on identifying main processes, external entities, and data flows.
            Return ONLY the Mermaid code without any markdown formatting or backticks.
            Rules:
            1. Start with 'flowchart TD'
            2. Use proper Mermaid syntax for nodes and connections
            3. Use square brackets [] for external entities
            4. Use round brackets () for processes
            5. Use curly brackets {{}} for data stores
            6. Use --> for connections
            7. Keep the diagram simple and clear
            8. Do not include any markdown formatting or backticks in the response
            9. Use simple node IDs like A, B, C, etc.
            10. Example format:
            flowchart TD
                A[User] --> B(Login Process)
                B --> C{{Database}}
                C --> B
                B --> A

            System description:
            {systemDescription}

            Important: Return ONLY the Mermaid code starting with 'flowchart TD' and containing the diagram definition. Do not include any other text or formatting.";

            var response = await MakeGeminiRequest(prompt);
            
            // Clean up the response
            var cleanedResponse = response
                .Replace("```mermaid", "")
                .Replace("```", "")
                .Trim();

            // If the response is empty or doesn't start with flowchart TD, return a default diagram
            if (string.IsNullOrWhiteSpace(cleanedResponse) || !cleanedResponse.StartsWith("flowchart TD"))
            {
                return @"flowchart TD
    A[User] --> B(Process)
    B --> C{{Data Store}}
    C --> B
    B --> A";
            }

            return cleanedResponse;
        }

        public async Task<string> GenerateAPIDoc(string systemDescription)
        {
            var prompt = $@"Given the following system description, generate OpenAPI (Swagger) documentation in JSON format.
            Include endpoints, request/response schemas, and descriptions. Format the response as valid OpenAPI JSON only.
            The response should be a valid JSON object starting with '{{' and ending with '}}'.
            Do not include any markdown formatting or backticks in the response.

            {systemDescription}";

            var response = await MakeGeminiRequest(prompt);
            
            // Clean up the response
            var cleanedResponse = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            // If the response is empty or invalid, return a default API doc
            if (string.IsNullOrWhiteSpace(cleanedResponse) || !cleanedResponse.StartsWith("{"))
            {
                return @"{
    ""openapi"": ""3.0.0"",
    ""info"": {
        ""title"": ""Default API"",
        ""version"": ""1.0.0"",
        ""description"": ""Default API documentation""
    },
    ""paths"": {}
}";
            }

            return cleanedResponse;
        }

        public async Task<string> GenerateDFDDoc(string systemDescription)
        {
            var prompt = $@"Given the following system description, generate detailed Data Flow Diagram (DFD) documentation in JSON format.
            Focus on identifying and documenting the data flows, processes, and entities in the system.
            Return a JSON object with the following structure:
            {{
                ""systemOverview"": ""A brief description of the system and its main purpose"",
                ""level0DFD"": ""Description of the Level 0 DFD showing the main processes and data flows"",
                ""externalEntities"": [
                    {{
                        ""name"": ""Name of the external entity (e.g., User, Customer, System)"",
                        ""description"": ""Description of the entity and its role"",
                        ""interactions"": ""How this entity interacts with the system""
                    }}
                ],
                ""processes"": [
                    {{
                        ""name"": ""Name of the process (e.g., Login, Process Order)"",
                        ""description"": ""Description of what the process does"",
                        ""inputs"": ""What data flows into this process"",
                        ""outputs"": ""What data flows out of this process""
                    }}
                ],
                ""dataStores"": [
                    {{
                        ""name"": ""Name of the data store (e.g., Database, File)"",
                        ""description"": ""Description of what data is stored"",
                        ""data"": ""Types of data stored in this data store""
                    }}
                ],
                ""dataFlows"": [
                    {{
                        ""from"": ""Source of the data flow"",
                        ""to"": ""Destination of the data flow"",
                        ""description"": ""Description of what data is being transferred"",
                        ""data"": ""Specific data items being transferred""
                    }}
                ],
                ""systemBoundaries"": ""Description of what is inside and outside the system""
            }}

            Example response for a simple login system:
            {{
                ""systemOverview"": ""A user authentication system that handles user login and registration."",
                ""level0DFD"": ""The system shows the interaction between users, the authentication process, and the user database."",
                ""externalEntities"": [
                    {{
                        ""name"": ""User"",
                        ""description"": ""End user trying to access the system"",
                        ""interactions"": ""Provides login credentials and receives authentication results""
                    }}
                ],
                ""processes"": [
                    {{
                        ""name"": ""Authentication Process"",
                        ""description"": ""Validates user credentials against stored data"",
                        ""inputs"": ""Username and password"",
                        ""outputs"": ""Authentication result and user session""
                    }}
                ],
                ""dataStores"": [
                    {{
                        ""name"": ""User Database"",
                        ""description"": ""Stores user credentials and information"",
                        ""data"": ""Username, password hash, user details""
                    }}
                ],
                ""dataFlows"": [
                    {{
                        ""from"": ""User"",
                        ""to"": ""Authentication Process"",
                        ""description"": ""Login credentials transfer"",
                        ""data"": ""Username and password""
                    }},
                    {{
                        ""from"": ""Authentication Process"",
                        ""to"": ""User Database"",
                        ""description"": ""Credential verification request"",
                        ""data"": ""Username and password hash""
                    }}
                ],
                ""systemBoundaries"": ""The system includes the authentication process and user database. External users interact with the system through a login interface.""
            }}

            System description:
            {systemDescription}

            Important: Return ONLY the JSON object with the DFD documentation. Do not include any other text or formatting.";

            var response = await MakeGeminiRequest(prompt);
            
            // Clean up the response
            var cleanedResponse = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            // If the response is empty or invalid, return a default DFD doc
            if (string.IsNullOrWhiteSpace(cleanedResponse) || !cleanedResponse.StartsWith("{"))
            {
                return @"{
    ""systemOverview"": ""Default system overview"",
    ""level0DFD"": ""Default Level 0 DFD description"",
    ""externalEntities"": [],
    ""processes"": [],
    ""dataStores"": [],
    ""dataFlows"": [],
    ""systemBoundaries"": ""Default system boundaries""
}";
            }

            return cleanedResponse;
        }

        private async Task<string> MakeGeminiRequest(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var url = $"{ApiVersion}/{Model}:generateContent?key={_apiKey}";
                _logger.LogInformation("Sending request to Gemini API: {Url}", url);

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {Response}", responseContent);
                    throw new Exception($"Gemini API error: {responseContent}");
                }

                _logger.LogInformation("Received successful response from Gemini API.");

                var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var text = json
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new Exception("Gemini response was empty or invalid.");
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Gemini API request");
                throw;
            }
        }
    }
}
