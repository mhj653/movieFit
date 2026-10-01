using System.Globalization;
using ProcessVideoAnalyzer.VlmContext.Steps;

namespace ProcessVideoAnalyzer.VlmContext;

public static class VlmContextRecipeFactory
{
    public static VlmContextRecipe FromLegacySettings(
        VlmContextCandidate candidate,
        VlmContextBlockSettings blockSettings)
    {
        var defaults = new VlmContextRecipeDefaults
        {
            ImageLongEdge = Math.Clamp(candidate.ImageLongEdge <= 0 ? blockSettings.CropOutputLongEdge : candidate.ImageLongEdge, 160, 1280),
            FrameCount = Math.Clamp(candidate.FrameCount <= 0 ? blockSettings.SamplingFrameCount : candidate.FrameCount, 1, 8),
            SamplingMode = string.IsNullOrWhiteSpace(candidate.SamplingMode) ? blockSettings.SamplingMode : candidate.SamplingMode,
            PromptMode = string.IsNullOrWhiteSpace(candidate.PromptMode) ? "description" : candidate.PromptMode,
            MaxOutputTokens = Math.Clamp(candidate.MaxOutputTokens <= 0 ? 120 : candidate.MaxOutputTokens, 32, 512),
            ResultLanguage = "ko",
            Temperature = 0.1
        };

        var recipe = new VlmContextRecipe
        {
            Id = string.IsNullOrWhiteSpace(candidate.AdvancedPresetId)
                ? Guid.NewGuid().ToString("N")
                : candidate.AdvancedPresetId,
            Name = string.IsNullOrWhiteSpace(candidate.AdvancedPresetName)
                ? $"{candidate.Name} Recipe"
                : candidate.AdvancedPresetName,
            Defaults = defaults,
            Steps = new List<VlmContextRecipeStep>
            {
                Step(
                    VlmContextStepTypes.Sampling,
                    "Frame Sampling",
                    ("mode", defaults.SamplingMode),
                    ("frameCount", defaults.FrameCount.ToString(CultureInfo.InvariantCulture))),
                Step(
                    VlmContextStepTypes.RoiFocus,
                    "ROI Focus",
                    ("mode", blockSettings.RoiMode),
                    ("x", blockSettings.RoiX.ToString(CultureInfo.InvariantCulture)),
                    ("y", blockSettings.RoiY.ToString(CultureInfo.InvariantCulture)),
                    ("width", blockSettings.RoiWidth.ToString(CultureInfo.InvariantCulture)),
                    ("height", blockSettings.RoiHeight.ToString(CultureInfo.InvariantCulture)),
                    ("padding", blockSettings.RoiPadding.ToString(CultureInfo.InvariantCulture))),
                Step(
                    VlmContextStepTypes.CropResize,
                    "Crop / Resize",
                    ("cropMode", blockSettings.CropMode),
                    ("outputLongEdge", defaults.ImageLongEdge.ToString(CultureInfo.InvariantCulture)),
                    ("keepAspect", blockSettings.CropKeepAspect ? "true" : "false"),
                    ("padding", blockSettings.CropPadding.ToString(CultureInfo.InvariantCulture))),
                Step(
                    VlmContextStepTypes.YoloHints,
                    "YOLO Hints",
                    ("enabled", (blockSettings.YoloEnabled || candidate.YoloHints) ? "true" : "false"),
                    ("labels", string.IsNullOrWhiteSpace(candidate.TargetLabels) ? blockSettings.YoloLabels : candidate.TargetLabels),
                    ("confidence", Math.Clamp(candidate.YoloConfidence <= 0 ? blockSettings.YoloConfidence : candidate.YoloConfidence, 0.05, 0.95).ToString(CultureInfo.InvariantCulture)),
                    ("runtime", blockSettings.YoloRuntime),
                    ("device", blockSettings.YoloDevice)),
                Step(
                    VlmContextStepTypes.OcrHints,
                    "OCR Hints",
                    ("enabled", (blockSettings.OcrEnabled || candidate.OcrHints) ? "true" : "false"),
                    ("engine", blockSettings.OcrEngine),
                    ("language", blockSettings.OcrLanguage),
                    ("useTextAsHint", blockSettings.OcrUseTextAsHint ? "true" : "false")),
                Step(
                    VlmContextStepTypes.PromptContext,
                    "Prompt Context",
                    ("focus", "visible manufacturing action"),
                    ("avoidGenericState", "true")),
                Step(
                    VlmContextStepTypes.LocalVlmAnalyze,
                    "Local VLM Analyze",
                    ("maxOutputTokens", defaults.MaxOutputTokens.ToString(CultureInfo.InvariantCulture))),
                Step(VlmContextStepTypes.ParseResult, "Parse Result")
            }
        };

        return recipe;
    }

    public static VlmContextBlockSettings ToLegacySettings(VlmContextRecipe? recipe, VlmContextBlockSettings fallback)
    {
        if (recipe is null)
        {
            return fallback;
        }

        var roi = recipe.Steps.FirstOrDefault(x => x.Type.Equals(VlmContextStepTypes.RoiFocus, StringComparison.OrdinalIgnoreCase));
        var crop = recipe.Steps.FirstOrDefault(x => x.Type.Equals(VlmContextStepTypes.CropResize, StringComparison.OrdinalIgnoreCase));
        var yolo = recipe.Steps.FirstOrDefault(x => x.Type.Equals(VlmContextStepTypes.YoloHints, StringComparison.OrdinalIgnoreCase));
        var ocr = recipe.Steps.FirstOrDefault(x => x.Type.Equals(VlmContextStepTypes.OcrHints, StringComparison.OrdinalIgnoreCase));
        var sampling = recipe.Steps.FirstOrDefault(x => x.Type.Equals(VlmContextStepTypes.Sampling, StringComparison.OrdinalIgnoreCase));

        return new VlmContextBlockSettings
        {
            YoloEnabled = SettingBool(yolo, "enabled", fallback.YoloEnabled),
            YoloModelPath = fallback.YoloModelPath,
            YoloRuntime = SettingString(yolo, "runtime", fallback.YoloRuntime),
            YoloDevice = SettingString(yolo, "device", fallback.YoloDevice),
            YoloInputSize = fallback.YoloInputSize,
            YoloConfidence = SettingDouble(yolo, "confidence", fallback.YoloConfidence),
            YoloIou = fallback.YoloIou,
            YoloLabels = SettingString(yolo, "labels", fallback.YoloLabels),
            YoloUseBoxesAsRoi = fallback.YoloUseBoxesAsRoi,
            YoloDrawOverlay = fallback.YoloDrawOverlay,
            RoiMode = SettingString(roi, "mode", fallback.RoiMode),
            RoiX = SettingInt(roi, "x", fallback.RoiX),
            RoiY = SettingInt(roi, "y", fallback.RoiY),
            RoiWidth = SettingInt(roi, "width", fallback.RoiWidth),
            RoiHeight = SettingInt(roi, "height", fallback.RoiHeight),
            RoiPadding = SettingDouble(roi, "padding", fallback.RoiPadding),
            CropMode = SettingString(crop, "cropMode", fallback.CropMode),
            CropPadding = SettingDouble(crop, "padding", fallback.CropPadding),
            CropKeepAspect = SettingBool(crop, "keepAspect", fallback.CropKeepAspect),
            CropOutputLongEdge = SettingInt(crop, "outputLongEdge", fallback.CropOutputLongEdge),
            OcrEnabled = SettingBool(ocr, "enabled", fallback.OcrEnabled),
            OcrEngine = SettingString(ocr, "engine", fallback.OcrEngine),
            OcrLanguage = SettingString(ocr, "language", fallback.OcrLanguage),
            OcrUseTextAsHint = SettingBool(ocr, "useTextAsHint", fallback.OcrUseTextAsHint),
            SamplingMode = SettingString(sampling, "mode", fallback.SamplingMode),
            SamplingFrameCount = SettingInt(sampling, "frameCount", fallback.SamplingFrameCount),
            SamplingIncludeTimestamp = fallback.SamplingIncludeTimestamp,
            SamplingMotionPeakWindowSec = fallback.SamplingMotionPeakWindowSec
        };
    }

    private static VlmContextRecipeStep Step(string type, string name, params (string Key, string Value)[] settings)
    {
        return new VlmContextRecipeStep
        {
            Type = type,
            Name = name,
            Settings = settings.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string SettingString(VlmContextRecipeStep? step, string key, string fallback)
    {
        return step?.Settings.TryGetValue(key, out var value) == true && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static int SettingInt(VlmContextRecipeStep? step, string key, int fallback)
    {
        return step?.Settings.TryGetValue(key, out var value) == true &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static double SettingDouble(VlmContextRecipeStep? step, string key, double fallback)
    {
        return step?.Settings.TryGetValue(key, out var value) == true &&
               double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool SettingBool(VlmContextRecipeStep? step, string key, bool fallback)
    {
        if (step?.Settings.TryGetValue(key, out var value) != true ||
            string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }
}
