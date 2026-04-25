namespace gAPI.SimpleRazorCompiler;

public class SimpleRazorCompiler
{
    public Token[] Tokenize(string razorTemplate)
    {
        var position = 0;
        while (position < razorTemplate.Length)
        {
            var currentChar = razorTemplate[position];
            var currentType = 

            // Implement tokenization logic based on the current character
            // For example, you can check for specific Razor syntax like @, {, }, etc.
            position++;
        }
        // Implement tokenization logic here
        return new Token[0];
    }
}
public enum TokenType
{
    Text,
    Code,
    Expression,
    // Add more token types as needed
}
