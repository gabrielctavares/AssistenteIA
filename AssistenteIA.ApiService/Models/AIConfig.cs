namespace AssistenteIA.ApiService.Models;
public class AIConfig
{
    public string Provider { get; set; } = default!;

    public OpenAIConfig OpenAI { get; set; } = new OpenAIConfig();

    public AzureConfig Azure { get; set; } = new AzureConfig();

    public OllamaConfig Ollama { get; set; } = new OllamaConfig();

}
public class OpenAIConfig
{
    public string ApiKey { get; set; } = default!;
    public string Uri { get; set; } = default!;
    public string Model { get; set; } = default!;

}
public class AzureConfig
{
    public string ApiKey { get; set; } = default!;
    public string Uri { get; set; } = default!;
    public string DeploymentName { get; set; } = default!;
}

public class OllamaConfig
{
    public string Uri { get; set; } = default!;
    public string Model { get; set; } = default!;
}
