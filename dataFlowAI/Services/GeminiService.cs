using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace dataFlowAI.Services
{
    public class GeminiService
    {
        private readonly List<string> _apiKeys = new List<string>
        {
            "AIzaSyA9VxC-PQ7uOI1xycf3sbrlqtwDtgxiQkw",
            "AIzaSyB4FP0bp8R5aE5Oh_p0CBlE3SSEtcqP1-M",
            "AIzaSyDnss4H0CbZJI9u0oZSB-ezDzZPxH2My0A"
        };
        private int _currentKeyIndex = 0;
        private readonly object _keyLock = new object();
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;

        // Use the latest supported Gemini model for content generation
        private const string Model = "models/gemini-1.5-flash";
        private const string ApiVersion = "v1beta";

        public GeminiService(ILogger<GeminiService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
            };
        }

        private string GetNextApiKey()
        {
            lock (_keyLock)
            {
                _currentKeyIndex = (_currentKeyIndex + 1) % _apiKeys.Count;
                return _apiKeys[_currentKeyIndex];
            }
        }

        private string GetCurrentApiKey()
        {
            return _apiKeys[_currentKeyIndex];
        }

        public async Task<string> GenerateDFD(string systemDescription)
        {
            var prompt = $@"Given the following system description, generate a Level 2 (detailed) Data Flow Diagram in Mermaid.js format.
            Focus on breaking down main processes into detailed sub-processes, showing all data transformations and interactions.
            Return ONLY the Mermaid code without any markdown formatting or backticks.
            Rules:
            1. Start with 'flowchart TD'
            2. Use proper Mermaid syntax for nodes and connections
            3. Use square brackets [] for external entities
            4. Use round brackets () for processes and sub-processes
            5. Use curly brackets {{}} for data stores
            6. Use --> for connections
            7. Break down each main process into its sub-processes
            8. Show all data transformations and validations
            9. Include all supporting services and data stores
            10. Use simple node IDs like A, B, C, etc.
            11. Group related processes and their sub-processes together
            12. Show bidirectional data flows where applicable
            13. Example format for a login system:
            flowchart TD
                A[User] --> B(Login Process)
                B --> C(Credential Validation)
                C --> D{{User Database}}
                D --> C
                C --> B
                B --> E(Session Management)
                E --> F{{Session Store}}
                F --> E
                E --> B
                B --> G(Authentication Service)
                G --> B
                B --> A

            System description:
            {systemDescription}

            Important: 
            1. Break down EACH main process into its detailed sub-processes
            2. Show ALL data validations and transformations
            3. Include ALL necessary data stores and services
            4. Return ONLY the Mermaid code starting with 'flowchart TD'
            5. Do not include any other text or formatting";

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
    A[User] --> B(Main Process)
    B --> C(Validation)
    C --> D{{Data Store}}
    D --> C
    C --> B
    B --> E(Processing)
    E --> F{{Cache}}
    F --> E
    E --> B
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
            Focus on documenting both Level 0 (context) and Level 2 (detailed) DFDs, including all processes, sub-processes, data flows, and their interactions.
            Return a JSON object with the following structure:
            {{
                ""systemOverview"": ""A detailed description of the system and its main purpose"",
                ""level0DFD"": ""Description of the Level 2 (Context) DFD showing the system's interaction with external entities"",
                ""level2DFD"": ""Description of the Level 2 DFD showing all processes and sub-processes"",
                ""externalEntities"": [
                    {{
                        ""name"": ""Name of the external entity"",
                        ""description"": ""Detailed description of the entity and its role"",
                        ""interactions"": ""All interactions with the system's processes and sub-processes""
                    }}
                ],
                ""processes"": [
                    {{
                        ""name"": ""Name of the process"",
                        ""description"": ""Detailed description of what the process does"",
                        ""subProcesses"": [
                            {{
                                ""name"": ""Name of the sub-process"",
                                ""description"": ""What this sub-process does"",
                                ""inputs"": ""Detailed input data description"",
                                ""outputs"": ""Detailed output data description"",
                                ""validations"": ""Data validations performed"",
                                ""transformations"": ""Data transformations performed""
                            }}
                        ],
                        ""inputs"": ""What data flows into this process"",
                        ""outputs"": ""What data flows out of this process""
                    }}
                ],
                ""dataStores"": [
                    {{
                        ""name"": ""Name of the data store"",
                        ""description"": ""Detailed description of what data is stored"",
                        ""data"": ""Types of data stored"",
                        ""accessPatterns"": ""How the data is accessed and by which processes""
                    }}
                ],
                ""dataFlows"": [
                    {{
                        ""from"": ""Source of the data flow"",
                        ""to"": ""Destination of the data flow"",
                        ""description"": ""Detailed description of the data flow"",
                        ""data"": ""Specific data items being transferred"",
                        ""validations"": ""Any validations performed on the data"",
                        ""transformations"": ""Any transformations applied to the data""
                    }}
                ],
                ""systemBoundaries"": ""Detailed description of system scope and boundaries""
            }}

            System description:
            {systemDescription}

            Important: 
            1. Include BOTH Level 2 (Context) and Level 2 (Detailed) DFD descriptions
            2. Document ALL processes and their sub-processes
            3. Detail ALL data flows and transformations
            4. Return ONLY the JSON object
            5. Do not include any other text or formatting";

            var response = await MakeGeminiRequest(prompt);
            
            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                return cleanedResponse;
            }
            catch (Exception)
            {
                // Return a default DFD doc with both Level 2 and Level 2 descriptions
                return @"{
    ""systemOverview"": ""Default system overview"",
    ""level0DFD"": ""Default Level 2 (Context) DFD description"",
    ""level2DFD"": ""Default Level 2 DFD description"",
    ""externalEntities"": [],
    ""processes"": [],
    ""dataStores"": [],
    ""dataFlows"": [],
    ""systemBoundaries"": ""Default system boundaries""
}";
            }
        }

        private string SanitizeEntityName(string name)
        {
            // Remove any invalid characters and ensure valid entity name
            return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray())
                .TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'); // Entity names can't start with numbers
        }

        private string SanitizeAttributeType(string type)
        {
            // Convert SQL types to Mermaid ER diagram types
            return type.ToLower() switch
            {
                var t when t.StartsWith("varchar") => "string",
                var t when t.StartsWith("int") => "int",
                var t when t.StartsWith("decimal") => "decimal",
                var t when t.StartsWith("date") => "date",
                var t when t.StartsWith("datetime") => "date",
                var t when t.StartsWith("boolean") => "boolean",
                var t when t.StartsWith("float") => "float",
                _ => "string"
            };
        }

        private string CleanJsonResponse(string response)
        {
            try
            {
                // Remove markdown code blocks and any whitespace
                var cleaned = response
                    .Replace("```json", "")
                    .Replace("```mermaid", "")
                    .Replace("```", "")
                    .Trim();

                // If the response starts with 'mermaid' or 'erDiagram', it's not JSON
                if (cleaned.StartsWith("mermaid") || cleaned.StartsWith("erDiagram"))
                {
                    throw new Exception("Response is in Mermaid format instead of JSON");
                }

                // Validate the cleaned JSON
                using (JsonDocument.Parse(cleaned))
                {
                    return cleaned;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON response from Gemini API");
                throw new Exception($"Failed to parse JSON response: {ex.Message}");
            }
        }

        private string ValidateERDiagram(string jsonDoc)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonDoc);
                var root = doc.RootElement;

                // Validate required properties exist
                if (!root.TryGetProperty("entities", out var entities) || 
                    !root.TryGetProperty("relationships", out var relationships))
                {
                    throw new Exception("JSON must contain 'entities' and 'relationships' arrays");
                }

                var sb = new StringBuilder();
                sb.AppendLine("erDiagram");

                // Add relationships
                foreach (var rel in relationships.EnumerateArray())
                {
                    var from = rel.GetProperty("from").GetString();
                    var to = rel.GetProperty("to").GetString();
                    var description = rel.GetProperty("description").GetString();
                    
                    sb.AppendLine($"    {from} ||--o{{ {to} : {description}");
                }

                // Add entities with their attributes
                foreach (var entity in entities.EnumerateArray())
                {
                    var name = entity.GetProperty("name").GetString();
                    sb.AppendLine($"    {name} {{");

                    var attributes = entity.GetProperty("attributes");
                    foreach (var attr in attributes.EnumerateArray())
                    {
                        var attrName = attr.GetProperty("name").GetString();
                        var attrType = attr.GetProperty("type").GetString();
                        var isPrimary = attr.GetProperty("isPrimary").GetBoolean();
                        var isForeign = attr.GetProperty("isForeign").GetBoolean();

                        var suffix = isPrimary ? " PK" : (isForeign ? " FK" : "");
                        sb.AppendLine($"        {attrType} {attrName}{suffix}");
                    }

                    sb.AppendLine("    }");
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating ER diagram");
                throw new Exception($"Failed to validate ER diagram: {ex.Message}");
            }
        }

        private string ConvertMermaidToJson(string mermaidDiagram)
        {
            try
            {
                var lines = mermaidDiagram.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                var entities = new List<Dictionary<string, object>>();
                var relationships = new List<Dictionary<string, object>>();
                
                Dictionary<string, object> currentEntity = null;
                List<Dictionary<string, object>> currentAttributes = null;

                foreach (var line in lines)
                {
                    // Skip the erDiagram line
                    if (line.Equals("erDiagram", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Parse relationships (lines containing }o--|| or similar)
                    if (line.Contains("--"))
                    {
                        var parts = line.Split(new[] { " : " }, StringSplitOptions.RemoveEmptyEntries);
                        var relationParts = parts[0].Split(new[] { " }o--|| ", " ||--o{ ", " ||--|| ", " }o--o{ " }, StringSplitOptions.RemoveEmptyEntries);
                        
                        if (relationParts.Length >= 2)
                        {
                            var from = relationParts[0].Trim();
                            var to = relationParts[1].Trim();
                            var description = parts.Length > 1 ? parts[1].Trim().Trim('"') : "";

                            relationships.Add(new Dictionary<string, object>
                            {
                                { "from", from },
                                { "to", to },
                                { "type", "one-to-many" },
                                { "description", description },
                                { "cardinality", "1:N" }
                            });
                        }
                        continue;
                    }

                    // Parse entity definitions
                    if (line.EndsWith("{"))
                    {
                        var entityName = line.Split('{')[0].Trim();
                        currentEntity = new Dictionary<string, object>
                        {
                            { "name", entityName },
                            { "description", $"Represents a {entityName.ToLower()} in the system" }
                        };
                        currentAttributes = new List<Dictionary<string, object>>();
                        continue;
                    }

                    // Parse entity closing
                    if (line == "}")
                    {
                        if (currentEntity != null && currentAttributes != null)
                        {
                            currentEntity["attributes"] = currentAttributes;
                            entities.Add(currentEntity);
                            currentEntity = null;
                            currentAttributes = null;
                        }
                        continue;
                    }

                    // Parse attributes
                    if (currentAttributes != null && !line.Contains("--"))
                    {
                        var parts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var type = parts[0].Trim().ToLower();
                            var name = parts[1].Trim();
                            var isPrimary = parts.Length > 2 && parts[2] == "PK";
                            var isForeign = parts.Length > 2 && parts[2] == "FK";

                            currentAttributes.Add(new Dictionary<string, object>
                            {
                                { "name", name },
                                { "type", type },
                                { "isPrimary", isPrimary },
                                { "isForeign", isForeign },
                                { "description", $"The {name.ToLower()} of the {currentEntity["name"]}" }
                            });
                        }
                    }
                }

                var documentation = new Dictionary<string, object>
                {
                    { "entities", entities },
                    { "relationships", relationships }
                };

                return JsonSerializer.Serialize(documentation, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting Mermaid diagram to JSON");
                throw new Exception($"Failed to convert diagram to JSON: {ex.Message}");
            }
        }

        private string ConvertJsonToMermaid(string jsonDoc)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonDoc);
                var root = doc.RootElement;

                if (!root.TryGetProperty("entities", out var entities) || 
                    !root.TryGetProperty("relationships", out var relationships))
                {
                    throw new Exception("JSON must contain 'entities' and 'relationships' arrays");
                }

                var sb = new StringBuilder();
                sb.AppendLine("erDiagram");

                // Add entities with their attributes first
                foreach (var entity in entities.EnumerateArray())
                {
                    var name = entity.GetProperty("name").GetString();
                    // Remove any spaces or special characters from entity name
                    name = new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());
                    
                    sb.AppendLine($"    {name} {{");

                    var attributes = entity.GetProperty("attributes");
                    foreach (var attr in attributes.EnumerateArray())
                    {
                        var attrName = attr.GetProperty("name").GetString();
                        // Remove any spaces or special characters from attribute name
                        attrName = new string(attrName.Where(c => char.IsLetterOrDigit(c)).ToArray());
                        
                        var attrType = attr.GetProperty("type").GetString().ToLower();
                        var isPrimary = attr.GetProperty("isPrimary").GetBoolean();
                        var isForeign = attr.GetProperty("isForeign").GetBoolean();

                        var suffix = isPrimary ? " PK" : (isForeign ? " FK" : "");
                        sb.AppendLine($"        {attrType} {attrName}{suffix}");
                    }

                    sb.AppendLine("    }");
                }

                // Add relationships after all entities
                foreach (var rel in relationships.EnumerateArray())
                {
                    var from = rel.GetProperty("from").GetString();
                    var to = rel.GetProperty("to").GetString();
                    // Remove any spaces or special characters from entity names
                    from = new string(from.Where(c => char.IsLetterOrDigit(c)).ToArray());
                    to = new string(to.Where(c => char.IsLetterOrDigit(c)).ToArray());
                    
                    var description = rel.GetProperty("description").GetString();
                    var type = rel.GetProperty("type").GetString()?.ToLower() ?? "one-to-many";

                    // Determine the relationship notation based on type
                    string notation = type switch
                    {
                        "one-to-many" => "||--o{",
                        "many-to-one" => "}o--||",
                        "one-to-one" => "||--||",
                        "many-to-many" => "}o--o{",
                        _ => "||--o{"
                    };

                    // Escape any special characters in the description
                    description = description.Replace("\"", "'");
                    sb.AppendLine($"    {from} {notation} {to} : \"{description}\"");
                }

                var diagram = sb.ToString().TrimEnd();
                _logger.LogInformation("Generated Mermaid diagram:\n{Diagram}", diagram);
                return diagram;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting JSON to Mermaid");
                throw new Exception($"Failed to convert JSON to Mermaid: {ex.Message}");
            }
        }

        public async Task<string> GenerateERD(string systemDescription)
        {
            var prompt = @"Generate an Entity Relationship Diagram in JSON format for the following system.
Use this exact format:
{
    ""entities"": [
        {
            ""name"": ""EntityName"",
            ""description"": ""Entity description"",
            ""attributes"": [
                {
                    ""name"": ""AttributeName"",
                    ""type"": ""int|string|date|decimal"",
                    ""isPrimary"": true|false,
                    ""isForeign"": true|false,
                    ""description"": ""Attribute description""
                }
            ]
        }
    ],
    ""relationships"": [
        {
            ""from"": ""EntityName"",
            ""to"": ""EntityName"",
            ""type"": ""one-to-many|many-to-one|one-to-one|many-to-many"",
            ""description"": ""Simple relationship description without special characters"",
            ""cardinality"": ""1:N|N:1|1:1|M:N""
        }
    ]
}

Rules:
1. Use proper data types (int, string, date, decimal)
2. Mark primary keys with isPrimary: true
3. Mark foreign keys with isForeign: true
4. Keep relationship descriptions simple and avoid special characters
5. Use only letters and numbers for entity and attribute names
6. Keep entities and attributes business-relevant
7. Ensure all relationships are properly defined
8. Include all necessary attributes for each entity
9. Follow standard database naming conventions
10. Use proper relationship types

System description: " + systemDescription;

            try
            {
                var response = await MakeGeminiRequest(prompt);

                // If we got a Mermaid diagram, validate and clean it
                if (response.StartsWith("erDiagram"))
                {
                    // Clean up the Mermaid diagram
                    var lines = response.Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => l.Replace("\"", "'")) // Replace double quotes with single quotes
                        .Select(l => new string(l.Where(c => !char.IsControl(c)).ToArray())); // Remove control characters

                    var cleanedDiagram = string.Join("\n", lines);
                    _logger.LogInformation("Cleaned Mermaid diagram:\n{Diagram}", cleanedDiagram);
                    return cleanedDiagram;
                }

                // Convert JSON to Mermaid
                return ConvertJsonToMermaid(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate ER documentation");
                throw new Exception($"Failed to generate documentation: {ex.Message}");
            }
        }

        public async Task<string> GenerateERDDoc(string systemDescription)
        {
            var prompt = @"Generate a detailed Entity Relationship documentation in JSON format for the following system.
IMPORTANT: Return ONLY a valid JSON object with this EXACT format, no additional text or formatting:

{
    ""entities"": [
        {
            ""name"": ""EntityName"",
            ""description"": ""Entity description"",
            ""attributes"": [
                {
                    ""name"": ""AttributeName"",
                    ""type"": ""int|string|date|decimal"",
                    ""isPrimary"": true|false,
                    ""isForeign"": true|false,
                    ""description"": ""Attribute description""
                }
            ]
        }
    ],
    ""relationships"": [
        {
            ""from"": ""EntityName"",
            ""to"": ""EntityName"",
            ""type"": ""one-to-many|many-to-one|one-to-one|many-to-many"",
            ""description"": ""Relationship description"",
            ""cardinality"": ""1:N|N:1|1:1|M:N""
        }
    ]
}

System description: " + systemDescription;

            try
            {
                var response = await MakeGeminiRequest(prompt);
                
                // Clean up any extra quotes or escapes
                response = response
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"")
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                // If we got a Mermaid diagram, convert it to JSON
                if (response.StartsWith("erDiagram"))
                {
                    var entities = new List<Dictionary<string, object>>();
                    var relationships = new List<Dictionary<string, object>>();
                    
                    var lines = response.Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

                    Dictionary<string, object> currentEntity = null;
                    List<Dictionary<string, object>> currentAttributes = null;

                    foreach (var line in lines.Skip(1)) // Skip erDiagram line
                    {
                        // Parse relationships (lines containing }o--|| or similar)
                        if (line.Contains("--") && line.Contains(":"))
                        {
                            var parts = line.Split(new[] { " : " }, StringSplitOptions.RemoveEmptyEntries);
                            var relationParts = parts[0].Split(new[] { " }o--|| ", " ||--o{ ", " ||--|| ", " }o--o{ " }, StringSplitOptions.RemoveEmptyEntries);
                            
                            if (relationParts.Length >= 2)
                            {
                                var from = relationParts[0].Trim();
                                var to = relationParts[1].Trim();
                                var description = parts.Length > 1 ? parts[1].Trim().Trim('"') : "";

                                relationships.Add(new Dictionary<string, object>
                                {
                                    { "from", from },
                                    { "to", to },
                                    { "type", "one-to-many" },
                                    { "description", description },
                                    { "cardinality", "1:N" }
                                });
                            }
                            continue;
                        }

                        // Parse entity definitions
                        if (line.EndsWith("{"))
                        {
                            var entityName = line.Split('{')[0].Trim();
                            currentEntity = new Dictionary<string, object>
                            {
                                { "name", entityName },
                                { "description", $"Represents a {entityName.ToLower()} in the system" }
                            };
                            currentAttributes = new List<Dictionary<string, object>>();
                            continue;
                        }

                        // Parse entity closing
                        if (line == "}")
                        {
                            if (currentEntity != null && currentAttributes != null)
                            {
                                currentEntity["attributes"] = currentAttributes;
                                entities.Add(currentEntity);
                                currentEntity = null;
                                currentAttributes = null;
                            }
                            continue;
                        }

                        // Parse attributes
                        if (currentAttributes != null && !line.Contains("--"))
                        {
                            var parts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var type = parts[0].Trim().ToLower();
                                var name = parts[1].Trim();
                                var isPrimary = parts.Length > 2 && parts[2] == "PK";
                                var isForeign = parts.Length > 2 && parts[2] == "FK";

                                currentAttributes.Add(new Dictionary<string, object>
                                {
                                    { "name", name },
                                    { "type", type },
                                    { "isPrimary", isPrimary },
                                    { "isForeign", isForeign },
                                    { "description", $"The {name.ToLower()} of the {currentEntity["name"]}" }
                                });
                            }
                        }
                    }

                    var documentation = new Dictionary<string, object>
                    {
                        { "entities", entities },
                        { "relationships", relationships }
                    };

                    return JsonSerializer.Serialize(documentation, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }

                // If we got JSON, validate it and return
                try
                {
                    using var doc = JsonDocument.Parse(response);
                    return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
                catch (JsonException)
                {
                    // If JSON is invalid, try one more time with a more explicit prompt
                    _logger.LogWarning("Invalid JSON response, retrying with modified prompt");
                    prompt += "\n\nIMPORTANT: The response MUST be a valid JSON object ONLY, no other text or formatting.";
                    response = await MakeGeminiRequest(prompt);
                    
                    // Clean and validate the retry response
                    response = response
                        .Replace("\\n", "\n")
                        .Replace("\\\"", "\"")
                        .Replace("```json", "")
                        .Replace("```", "")
                        .Trim();

                    using var retryDoc = JsonDocument.Parse(response);
                    return JsonSerializer.Serialize(retryDoc.RootElement, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate ER documentation");
                throw new Exception($"Failed to generate documentation: {ex.Message}");
            }
        }

        public async Task<string> GenerateSchemaD(string systemDescription)
        {
            var prompt = "Given the following system description, generate a Class/Schema Diagram in Mermaid.js format.\n" +
                "Focus on identifying classes, their attributes, methods, and relationships.\n" +
                "Return ONLY the Mermaid code without any markdown formatting or backticks.\n" +
                "Rules:\n" +
                "1. Start with 'classDiagram'\n" +
                "2. Use proper Mermaid class diagram syntax\n" +
                "3. Define classes with their attributes and methods\n" +
                "4. Show relationships between classes\n" +
                "5. Keep the diagram clear and readable\n" +
                "6. Do not include any markdown formatting or backticks\n" +
                "7. Example format:\n" +
                "classDiagram\n" +
                "    class User {\n" +
                "        -string id\n" +
                "        -string name\n" +
                "        -string email\n" +
                "        +validateEmail() bool\n" +
                "    }\n" +
                "    class Order {\n" +
                "        -string id\n" +
                "        -string status\n" +
                "        -datetime createdAt\n" +
                "        +process() void\n" +
                "    }\n" +
                "    User \"1\" -- \"*\" Order : places\n\n" +
                "System description:\n" +
                systemDescription + "\n\n" +
                "Important: Return ONLY the Mermaid code starting with 'classDiagram'. Do not include any other text or formatting.";

            var response = await MakeGeminiRequest(prompt);
            
            // Clean up the response
            var cleanedResponse = response
                .Replace("```mermaid", "")
                .Replace("```", "")
                .Trim();

            // If the response is empty or doesn't start with classDiagram, return a default diagram
            if (string.IsNullOrWhiteSpace(cleanedResponse) || !cleanedResponse.StartsWith("classDiagram"))
            {
                return "classDiagram\n" +
                    "    class User {\n" +
                    "        -string id\n" +
                    "        -string name\n" +
                    "        -string email\n" +
                    "        +validateEmail() bool\n" +
                    "    }\n" +
                    "    class Order {\n" +
                    "        -string id\n" +
                    "        -string status\n" +
                    "        -datetime createdAt\n" +
                    "        +process() void\n" +
                    "    }\n" +
                    "    User \"1\" -- \"*\" Order : places";
            }

            return cleanedResponse;
        }

        public async Task<string> GenerateSchemaDoc(string systemDescription)
        {
            var prompt = "Generate a detailed database schema documentation in valid JSON format.\n" +
                "The response must be a single, valid JSON object with this exact structure:\n" +
                "{\n" +
                "    \"databaseOverview\": \"string\",\n" +
                "    \"tables\": [\n" +
                "        {\n" +
                "            \"name\": \"string\",\n" +
                "            \"description\": \"string\",\n" +
                "            \"columns\": [\n" +
                "                {\n" +
                "                    \"name\": \"string\",\n" +
                "                    \"type\": \"string\",\n" +
                "                    \"constraints\": \"string\"\n" +
                "                }\n" +
                "            ],\n" +
                "            \"primaryKey\": \"string\",\n" +
                "            \"foreignKeys\": [\n" +
                "                {\n" +
                "                    \"column\": \"string\",\n" +
                "                    \"references\": \"string\"\n" +
                "                }\n" +
                "            ]\n" +
                "        }\n" +
                "    ]\n" +
                "}\n\n" +
                "System description:\n" +
                systemDescription + "\n\n" +
                "Important: Return ONLY the JSON object. No explanation, no markdown, just the JSON.";

            var response = await MakeGeminiRequest(prompt);
            
            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                return cleanedResponse;
            }
            catch (Exception)
            {
                // Return a valid default JSON if the response is invalid
                return JsonSerializer.Serialize(new
                {
                    databaseOverview = "Default database overview",
                    tables = new[]
                    {
                        new
                        {
                            name = "users",
                            description = "Stores user information",
                            columns = new[]
                            {
                                new
                                {
                                    name = "id",
                                    type = "string",
                                    constraints = "PRIMARY KEY"
                                }
                            },
                            primaryKey = "id",
                            foreignKeys = Array.Empty<object>()
                        }
                    }
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        private async Task<string> MakeGeminiRequest(string prompt)
        {
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
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
                                    new
                                    {
                                        text = prompt
                                    }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.1,
                            topP = 0.1,
                            topK = 16,
                            maxOutputTokens = 2048
                        }
                    };

                    var currentKey = GetCurrentApiKey();
                    var url = $"{ApiVersion}/{Model}:generateContent?key={currentKey}";
                    _logger.LogInformation("Sending request to Gemini API");

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        System.Text.Encoding.UTF8, 
                        "application/json"
                    );

                    var response = await _httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                            responseContent.Contains("quota") ||
                            responseContent.Contains("rate limit"))
                        {
                            _logger.LogWarning("API key quota exceeded, rotating to next key");
                            GetNextApiKey();
                            retryCount++;
                            continue;
                        }

                        _logger.LogError("Gemini API error: {Response}", responseContent);
                        throw new Exception($"Gemini API error: {responseContent}");
                    }

                    _logger.LogInformation("Received successful response from Gemini API");

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

                    // Clean up the response
                    text = text
                        .Replace("\\n", "\n")
                        .Replace("\\\"", "\"")
                        .Replace("```json", "")
                        .Replace("```", "")
                        .Trim();

                    // Validate JSON if it looks like JSON
                    if (text.StartsWith("{"))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(text);
                            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions 
                            { 
                                WriteIndented = true 
                            });
                        }
                        catch (JsonException)
                        {
                            _logger.LogWarning("Invalid JSON response, retrying with modified prompt");
                            return await MakeGeminiRequest(prompt + "\n\nIMPORTANT: Return ONLY valid JSON without any additional text or formatting.");
                        }
                    }

                    return text;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during Gemini API request");
                    if (retryCount < maxRetries - 1)
                    {
                        _logger.LogInformation("Retrying with next API key");
                        GetNextApiKey();
                        retryCount++;
                        continue;
                    }
                    throw;
                }
            }

            throw new Exception("All API keys exhausted or maximum retries reached");
        }
    }
}
