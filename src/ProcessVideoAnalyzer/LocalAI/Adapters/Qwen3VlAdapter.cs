using ProcessVideoAnalyzer.LocalAI.Core;

namespace ProcessVideoAnalyzer.LocalAI.Adapters;

public sealed class Qwen3VlAdapter : GenericLlamaCppVisionAdapter
{
    public override string AdapterId => "qwen3vl";

    public override bool CanHandle(VlmModelProfile profile)
    {
        return string.Equals(profile.Adapter, AdapterId, StringComparison.OrdinalIgnoreCase);
    }
}
