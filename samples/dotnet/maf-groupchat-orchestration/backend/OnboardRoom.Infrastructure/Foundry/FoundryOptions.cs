// Copyright (c) Microsoft. All rights reserved.

namespace OnboardRoom.Infrastructure.Foundry;

public sealed class FoundryOptions
{
    public const string ModelDeploymentName = "gpt-5.4";

    public string ProjectEndpoint { get; set; } = string.Empty;

    public string DeploymentName { get; } = ModelDeploymentName;

    public string ToolboxName { get; set; } = "onboardroom-toolbox";

    public string ToolboxApiVersion { get; set; } = "v1";
}
