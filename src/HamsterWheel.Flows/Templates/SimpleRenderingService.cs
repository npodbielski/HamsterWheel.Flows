using System.Text;
using HamsterWheel.HLinq;

namespace HamsterWheel.Flows.Templates;

public class SimpleRenderingService : IRenderingService
{
    public Task<string> Render(string template, Dictionary<string, object?> models,
        CancellationToken token = default)
    {
        var span = template.AsSpan();
        var renderedBuilder = new StringBuilder();
        var chunkIndexStart = 0;
        var chunkIndexEnd = 0;
        for (var i = 0; i < span.Length; i++)
        {
            var nextTwoChars = span.Length > i+1 ? span[i..(i + 2)] : null;
            switch (nextTwoChars)
            {
                case "{{":
                    chunkIndexEnd = i;
                    //insert string as is as the previous part was just constant
                    renderedBuilder.Append(span[chunkIndexStart..chunkIndexEnd]);
                    chunkIndexStart = i;
                    break;
                case "}}":
                {
                    chunkIndexEnd = i;
                    var modelName = span[(chunkIndexStart + 2)..chunkIndexEnd].Trim();
                    var modelKey = modelName.ToString();
                    string? propExpression = null;
                    if (modelName.Contains('.'))
                    {
                        var firstDotIndex = modelName.IndexOf('.');
                        modelKey = modelName[..firstDotIndex].ToString();
                        propExpression = modelName[firstDotIndex..].ToString();
                    }

                    if (models.TryGetValue(modelKey, out var model))
                    {
                        var modelValue = model;
                        if (propExpression != null)
                        {
                            modelValue = modelValue.ExecuteHLinq($"x{propExpression}");
                        }

                        renderedBuilder.Append(modelValue);
                    }

                    chunkIndexStart = i + 1;
                    break;
                }
            }
        }

        if (chunkIndexEnd != 0 && chunkIndexEnd != span.Length)
        {
            renderedBuilder.Append(span[(chunkIndexEnd+2)..]);
        }

        return Task.FromResult(chunkIndexStart == 0 ? template : renderedBuilder.ToString());
    }
}
