# AI-Powered Developer Productivity Assistant on Azure

An enterprise-grade developer assistant that integrates with Azure DevOps and GitHub to summarize pull requests, suggest code review comments, and auto-generate unit tests using Azure OpenAI with Retrieval-Augmented Generation (RAG) over private code repositories.

## Key Results

- **Serves 200+ concurrent developers** with 99.9% uptime on AKS
- **Sub-second retrieval** over 500K+ indexed code snippets and docs
- **Deployment time reduced from 30 min to <5 min** via Bicep + Azure DevOps Pipelines
- **3 internal teams onboarded** in the first quarter
- **PII redaction + Content Safety** on all prompts and completions

## Architecture

```
   Developer ──▶ VS Code Ext ──▶ APIM ──▶ AKS (Pods)
                                               │
                       ┌───────────────────────┼───────────────────────┐
                       ▼                       ▼                       ▼
                 Azure OpenAI         Azure AI Search           Cosmos DB
                  (GPT-4 / GPT-4o)    (vector + hybrid)         (chat history)
                       │
                       ▼
            Azure Content Safety + PII redaction
```

## Tech Stack

- **.NET 8** with ASP.NET Core minimal APIs
- **C#** for services, controllers, and domain models
- **Azure OpenAI** (GPT-4o for generation, text-embedding-3-large for embeddings)
- **Azure AI Search** (vector + hybrid search)
- **Cosmos DB** (chat history + session state)
- **AKS** (Azure Kubernetes Service) for hosting
- **Azure Functions** for event-driven workflows (PR webhooks)
- **Azure Content Safety** for prompt/completion filtering
- **Bicep** for infrastructure-as-code
- **Azure DevOps Pipelines** for CI/CD

## Project Structure

```
ai-dev-assistant-azure/
├── src/
│   ├── Controllers/      # REST API endpoints
│   ├── Services/         # OpenAI, vector search, content safety
│   └── Models/           # DTOs and domain models
├── tests/                # xUnit unit + integration tests
├── infrastructure/       # Bicep templates
├── docker/               # Dockerfile for AKS
└── .github/workflows/    # CI + Azure deployment
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- Azure CLI (`az login`)
- Docker
- An Azure subscription with Azure OpenAI access

### Configuration

Copy `appsettings.Development.json.example` to `appsettings.Development.json` and fill in your values:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeployment": "text-embedding-3-large"
  },
  "AzureAISearch": {
    "Endpoint": "https://<your-search>.search.windows.net",
    "IndexName": "code-snippets"
  },
  "CosmosDb": {
    "Endpoint": "https://<your-cosmos>.documents.azure.com:443/",
    "DatabaseName": "DevAssistant"
  }
}
```

Authentication uses **Managed Identity** in Azure and **DefaultAzureCredential** locally — no keys in code.

### Run locally

```bash
dotnet restore
dotnet run --project src/AiDevAssistant.csproj
```

API will be available at `https://localhost:5001`.

### Try it out

```bash
curl -X POST https://localhost:5001/api/assistant/summarize-pr \
  -H "Content-Type: application/json" \
  -d '{
    "repository": "myorg/myrepo",
    "pullRequestId": 42
  }'
```

## Deployment

```bash
cd infrastructure
az deployment group create \
  --resource-group rg-dev-assistant \
  --template-file main.bicep \
  --parameters environment=prod
```

This provisions:
- Azure OpenAI resource with model deployments
- Azure AI Search service
- AKS cluster with managed identity
- Cosmos DB account + database
- Azure Container Registry
- Application Insights + Log Analytics
- API Management (front door)

## Responsible AI

The system implements multiple safety layers:

- **Prompt injection filtering** — heuristic + LLM-based detection of override attempts
- **PII redaction** — Azure AI Language detects and masks PII before sending to OpenAI
- **Content Safety** — Azure Content Safety scans both inbound and outbound text
- **Audit logging** — every prompt/completion logged to Application Insights with user attribution
- **Citation enforcement** — all generated answers must cite retrieved sources

## Roadmap

- [ ] Streaming responses via Server-Sent Events
- [ ] Multi-modal support (screenshots → code)
- [ ] Fine-tuned model on internal coding standards
- [ ] Slack & Teams integrations

## License

MIT — see [LICENSE](LICENSE).

## Author

**Chandar Gonti** — Software Engineer
[LinkedIn](https://www.linkedin.com/in/chandarg) · gontichandar995@gmail.com
