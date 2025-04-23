using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace RemoteAgents.Web;

public class MarkdownModel : ComponentBase
{
    private string content;

    [Inject] public IHtmlSanitizer HtmlSanitizer { get; set; }

    [Parameter]
    public string Content
    {
        get => content;
        set
        {
            content = value;
            HtmlContent = ConvertStringToMarkupString(content);
        }
    }

    public MarkupString HtmlContent { get; private set; }

    private MarkupString ConvertStringToMarkupString(string value)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            var html = Markdig.Markdown.ToHtml(value, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());

            var sanitizedHtml = HtmlSanitizer.Sanitize(html);

            return new MarkupString(sanitizedHtml);
        }

        return new MarkupString();
    }
}
