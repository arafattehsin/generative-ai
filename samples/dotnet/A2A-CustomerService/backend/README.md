# A2A Customer Service Configuration

## Environment Variables

Set these environment variables to use your Azure OpenAI service:

```bash
AOI_ENDPOINT_SWDN=https://your-openai-resource.openai.azure.com/
AOI_KEY_SWDN=your-api-key-here
```

## Runtime Toggle

Switch between mock and real implementations:
- **Frontend**: Use the toggle switch in the "Implementation Mode" card
- **API**: `POST /api/customerservice/toggle-implementation`

## Configuration Priority

1. Runtime toggle (highest)
2. Environment variables  
3. appsettings.json (fallback)

Perfect for .NET! 🚀
