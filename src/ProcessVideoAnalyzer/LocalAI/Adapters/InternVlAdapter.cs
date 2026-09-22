using ProcessVideoAnalyzer.LocalAI.Core;

namespace ProcessVideoAnalyzer.LocalAI.Adapters;

public sealed class InternVlAdapter : GenericLlamaCppVisionAdapter
{
    public override string AdapterId => "internvl";

    public override bool CanHandle(VlmModelProfile profile)
    {
        return string.Equals(profile.Adapter, AdapterId, StringComparison.OrdinalIgnoreCase);
    }
}
