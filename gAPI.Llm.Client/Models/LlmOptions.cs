namespace gAPI.Llm.Client.Models;

/// <summary>
/// Runtime configuration options for the LLM chat request.
/// </summary>
public class LlmOptions
{
    /// <summary>
    /// Controls whether the model's chain-of-thought thinking trace is returned/processed.
    /// Primarily used for thinking-capable models like DeepSeek-R1.
    /// </summary>
    public bool? Think { get; set; }

    /// <summary>
    /// The temperature of the model. Higher values (e.g., 0.8) make the output more creative/random,
    /// while lower values (e.g., 0.2) make it more focused and deterministic.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Context window size (in tokens). This defines how much past conversation/history the model can remember.
    /// If not specified, defaults to the model's default <see cref="Model.MaxTokenSize"/> or 8192.
    /// </summary>
    public int? NumCtx { get; set; }

    /// <summary>
    /// Sets the random number seed for generation. Setting this to a specific number
    /// makes the model's responses deterministic/reproducible.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Reduces the probability of generating nonsense. Only the top K tokens with the highest
    /// probabilities are considered (e.g., default is 40).
    /// </summary>
    public int? TopK { get; set; }

    /// <summary>
    /// Works together with TopK. Only tokens whose cumulative probability exceeds P (e.g., 0.9)
    /// are considered, balancing diversity and coherence.
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Maximum number of tokens to predict/generate in the response.
    /// </summary>
    public int? NumPredict { get; set; }

    /// <summary>
    /// Penalizes repetition. A higher value (e.g., 1.5) strongly discourages repeating the same phrases.
    /// </summary>
    public double? RepeatPenalty { get; set; }

    /// <summary>
    /// Custom stop sequences. When the model encounters one of these sequences, it stops generating text.
    /// </summary>
    public string[]? Stop { get; set; }
}
