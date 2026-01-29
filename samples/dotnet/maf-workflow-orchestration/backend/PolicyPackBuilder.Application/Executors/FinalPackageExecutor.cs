// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using System.Web;
using PolicyPackBuilder.Domain.ValueObjects;

namespace PolicyPackBuilder.Application.Executors;

/// <summary>
/// Step 6: Generate the final HTML package.
/// This is a non-LLM step that formats the output.
/// </summary>
public sealed class FinalPackageExecutor : IStepExecutor
{
    /// <inheritdoc />
    public string StepName => WorkflowSteps.FinalPackage;

    /// <inheritdoc />
    public Task<WorkflowContext> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default)
    {
        StringBuilder html = new();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("    <title>PolicyPack - Generated Document</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        :root {");
        html.AppendLine("            --primary-color: #2563eb;");
        html.AppendLine("            --secondary-color: #64748b;");
        html.AppendLine("            --success-color: #22c55e;");
        html.AppendLine("            --warning-color: #f59e0b;");
        html.AppendLine("            --background-color: #f8fafc;");
        html.AppendLine("            --card-background: #ffffff;");
        html.AppendLine("            --text-primary: #1e293b;");
        html.AppendLine("            --text-secondary: #64748b;");
        html.AppendLine("            --border-color: #e2e8f0;");
        html.AppendLine("        }");
        html.AppendLine("        * { box-sizing: border-box; margin: 0; padding: 0; }");
        html.AppendLine("        body {");
        html.AppendLine("            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;");
        html.AppendLine("            line-height: 1.6;");
        html.AppendLine("            color: var(--text-primary);");
        html.AppendLine("            background-color: var(--background-color);");
        html.AppendLine("            padding: 2rem;");
        html.AppendLine("        }");
        html.AppendLine("        .container {");
        html.AppendLine("            max-width: 800px;");
        html.AppendLine("            margin: 0 auto;");
        html.AppendLine("        }");
        html.AppendLine("        .header {");
        html.AppendLine("            background: linear-gradient(135deg, var(--primary-color), #3b82f6);");
        html.AppendLine("            color: white;");
        html.AppendLine("            padding: 2rem;");
        html.AppendLine("            border-radius: 12px 12px 0 0;");
        html.AppendLine("            margin-bottom: 0;");
        html.AppendLine("        }");
        html.AppendLine("        .header h1 { font-size: 1.75rem; margin-bottom: 0.5rem; }");
        html.AppendLine("        .header .meta { opacity: 0.9; font-size: 0.875rem; }");
        html.AppendLine("        .content {");
        html.AppendLine("            background: var(--card-background);");
        html.AppendLine("            padding: 2rem;");
        html.AppendLine("            border: 1px solid var(--border-color);");
        html.AppendLine("            border-top: none;");
        html.AppendLine("        }");
        html.AppendLine("        .section {");
        html.AppendLine("            margin-bottom: 1.5rem;");
        html.AppendLine("            padding-bottom: 1.5rem;");
        html.AppendLine("            border-bottom: 1px solid var(--border-color);");
        html.AppendLine("        }");
        html.AppendLine("        .section:last-child { border-bottom: none; margin-bottom: 0; padding-bottom: 0; }");
        html.AppendLine("        .section-title {");
        html.AppendLine("            font-size: 1.125rem;");
        html.AppendLine("            font-weight: 600;");
        html.AppendLine("            color: var(--primary-color);");
        html.AppendLine("            margin-bottom: 0.75rem;");
        html.AppendLine("            display: flex;");
        html.AppendLine("            align-items: center;");
        html.AppendLine("            gap: 0.5rem;");
        html.AppendLine("        }");
        html.AppendLine("        .section-content { white-space: pre-wrap; }");
        html.AppendLine("        .badge {");
        html.AppendLine("            display: inline-block;");
        html.AppendLine("            padding: 0.25rem 0.75rem;");
        html.AppendLine("            border-radius: 9999px;");
        html.AppendLine("            font-size: 0.75rem;");
        html.AppendLine("            font-weight: 500;");
        html.AppendLine("            text-transform: uppercase;");
        html.AppendLine("        }");
        html.AppendLine("        .badge-primary { background: #dbeafe; color: #1e40af; }");
        html.AppendLine("        .badge-secondary { background: #f1f5f9; color: #475569; }");
        html.AppendLine("        .warnings {");
        html.AppendLine("            background: #fef3c7;");
        html.AppendLine("            border: 1px solid #f59e0b;");
        html.AppendLine("            border-radius: 8px;");
        html.AppendLine("            padding: 1rem;");
        html.AppendLine("            margin-top: 1rem;");
        html.AppendLine("        }");
        html.AppendLine("        .warnings-title { color: #92400e; font-weight: 600; margin-bottom: 0.5rem; }");
        html.AppendLine("        .warnings ul { margin-left: 1.25rem; color: #92400e; }");
        html.AppendLine("        .footer {");
        html.AppendLine("            background: var(--card-background);");
        html.AppendLine("            padding: 1rem 2rem;");
        html.AppendLine("            border: 1px solid var(--border-color);");
        html.AppendLine("            border-top: none;");
        html.AppendLine("            border-radius: 0 0 12px 12px;");
        html.AppendLine("            font-size: 0.75rem;");
        html.AppendLine("            color: var(--text-secondary);");
        html.AppendLine("            text-align: center;");
        html.AppendLine("        }");
        html.AppendLine("        @media print {");
        html.AppendLine("            body { background: white; padding: 0; }");
        html.AppendLine("            .container { max-width: none; }");
        html.AppendLine("        }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");

        // Header
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine("            <h1>📋 PolicyPack Document</h1>");
        html.AppendLine($"            <div class=\"meta\">Generated on {DateTime.UtcNow:MMMM dd, yyyy 'at' HH:mm} UTC</div>");
        html.AppendLine("            <div class=\"meta\" style=\"margin-top: 0.5rem;\">");
        html.AppendLine($"                <span class=\"badge badge-primary\">{context.Options.Audience}</span>");
        html.AppendLine($"                <span class=\"badge badge-secondary\">{context.Options.Tone} Tone</span>");
        if (context.Options.StrictCompliance)
        {
            html.AppendLine("                <span class=\"badge badge-secondary\">Strict Compliance</span>");
        }
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Main content
        html.AppendLine("        <div class=\"content\">");

        // Final document section
        html.AppendLine("            <div class=\"section\">");
        html.AppendLine("                <h2 class=\"section-title\">📄 Final Document</h2>");
        html.AppendLine($"                <div class=\"section-content\">{HttpUtility.HtmlEncode(context.ToneRewrittenText)}</div>");
        html.AppendLine("            </div>");

        // Warnings section (if any)
        if (context.Warnings.Count > 0)
        {
            html.AppendLine("            <div class=\"warnings\">");
            html.AppendLine("                <div class=\"warnings-title\">⚠️ Processing Warnings</div>");
            html.AppendLine("                <ul>");
            foreach (string warning in context.Warnings)
            {
                html.AppendLine($"                    <li>{HttpUtility.HtmlEncode(warning)}</li>");
            }
            html.AppendLine("                </ul>");
            html.AppendLine("            </div>");
        }

        html.AppendLine("        </div>");

        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine("            Generated by PolicyPack Builder • Microsoft Agent Framework Sequential Workflow Demo");
        html.AppendLine("        </div>");

        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        context.FinalHtml = html.ToString();

        return Task.FromResult(context);
    }
}
