using gAPI.AutoComponents.SimpleRazorCompiler.Enums;

namespace gAPI.AutoComponents.SimpleRazorCompiler;

public class CodeNode : Node
{
    public CodeNode(string markup) : base(null, NodeTypeEnum.Top)
    {
        var position = 0;
        position = ReadBody(markup, position);
    }
}
