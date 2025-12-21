using gAPI.AutoComponent.SimpleRazorCompiler.Enums;

namespace gAPI.AutoComponent.SimpleRazorCompiler;

public class CodeNode : Node
{
    public CodeNode(string markup) : base(null, NodeTypeEnum.Top)
    {
        var position = 0;
        position = ReadBody(markup, position);
    }
}
