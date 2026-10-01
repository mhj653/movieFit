namespace ProcessVideoAnalyzer.VlmContext.Steps;

public static class VlmContextStepRegistry
{
    private static readonly IReadOnlyList<VlmContextStepDefinition> BuiltInDefinitions =
        new List<VlmContextStepDefinition>
        {
            new()
            {
                Type = VlmContextStepTypes.Sampling,
                Name = "Frame Sampling",
                Category = "Input",
                Description = "Select representative frames from an image or video segment.",
                Inputs = new[] { "frames" },
                Outputs = new[] { "selectedFrames" },
                DefaultSettings = new Dictionary<string, string>
                {
                    ["mode"] = "uniform",
                    ["frameCount"] = "1"
                }
            },
            new()
            {
                Type = VlmContextStepTypes.RoiFocus,
                Name = "ROI Focus",
                Category = "Vision Prep",
                Description = "Store ROI information for crop and prompt context.",
                Inputs = new[] { "image" },
                Outputs = new[] { "roi" },
                DefaultSettings = new Dictionary<string, string>
                {
                    ["mode"] = "fullFrame",
                    ["x"] = "0",
                    ["y"] = "0",
                    ["width"] = "0",
                    ["height"] = "0",
                    ["padding"] = "0.12"
                }
            },
            new()
            {
                Type = VlmContextStepTypes.CropResize,
                Name = "Crop / Resize",
                Category = "Vision Prep",
                Description = "Prepare VLM input images using crop mode and image size defaults.",
                Inputs = new[] { "selectedFrames", "roi" },
                Outputs = new[] { "preparedFrames" },
                DefaultSettings = new Dictionary<string, string>
                {
                    ["cropMode"] = "fullFrame",
                    ["outputLongEdge"] = "768",
                    ["keepAspect"] = "true",
                    ["padding"] = "0.12"
                }
            },
            new()
            {
                Type = VlmContextStepTypes.YoloHints,
                Name = "YOLO Hints",
                Category = "AI Context",
                Description = "Reserve object detection settings and add detector hints to VLM context.",
                Inputs = new[] { "preparedFrames" },
                Outputs = new[] { "contextText" },
                DefaultSettings = new Dictionary<string, string>
                {
                    ["enabled"] = "false",
                    ["labels"] = "worker,hand,part,fixture,button,tool,machine",
                    ["confidence"] = "0.45",
                    ["runtime"] = "onnxruntime",
                    ["device"] = "cpu"
                }
            },
            new()
            {
                Type = VlmContextStepTypes.OcrHints,
                Name = "OCR Hints",
                Category = "AI Context",
                Description = "Reserve OCR settings and add text-reading hints to VLM context.",
                Inputs = new[] { "preparedFrames" },
                Outputs = new[] { "contextText" },
                DefaultSettings = new Dictionary<string, string>
                {
                    ["enabled"] = "false",
                    ["engine"] = "future",
                    ["language"] = "ko",
                    ["useTextAsHint"] = "true"
                }
            },
            new()
            {
                Type = VlmContextStepTypes.PromptContext,
                Name = "Prompt Context",
                Category = "AI Context",
                Description = "Build the text context sent to the VLM.",
                Inputs = new[] { "preparedFrames", "contextText" },
                Outputs = new[] { "promptHints" },
                DefaultSettings = new Dictionary<string, string>
                {
                    ["focus"] = "visible manufacturing action",
                    ["avoidGenericState"] = "true"
                }
            },
            new()
            {
                Type = VlmContextStepTypes.LocalVlmAnalyze,
                Name = "Local VLM Analyze",
                Category = "AI",
                Description = "Terminal step executed by the Local VLM adapter.",
                Inputs = new[] { "preparedFrames", "promptHints" },
                Outputs = new[] { "description", "rawResponse" },
                DefaultSettings = new Dictionary<string, string>
                {
                    ["model"] = "active",
                    ["maxOutputTokens"] = "120"
                }
            },
            new()
            {
                Type = VlmContextStepTypes.ParseResult,
                Name = "Parse Result",
                Category = "AI",
                Description = "Map VLM output into description and quality fields.",
                Inputs = new[] { "rawResponse" },
                Outputs = new[] { "description" },
                DefaultSettings = new Dictionary<string, string>()
            }
        };

    public static IReadOnlyList<VlmContextStepDefinition> ListBuiltIns() => BuiltInDefinitions;

    public static VlmContextStepDefinition? Find(string type)
    {
        return BuiltInDefinitions.FirstOrDefault(x => x.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
    }
}
