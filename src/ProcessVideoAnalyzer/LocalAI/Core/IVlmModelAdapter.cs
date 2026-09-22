namespace ProcessVideoAnalyzer.LocalAI.Core;

public interface IVlmModelAdapter
{
    string AdapterId { get; }
    bool CanHandle(VlmModelProfile profile);
    VlmPreparedRequest Prepare(VlmRequest request, VlmModelProfile profile);
    VlmResult Parse(VlmRawResponse response, VlmModelProfile profile);
}
