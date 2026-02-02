using gAPI.AutoPage.SimpleRazorCompiler.Enums;

namespace gAPI.AutoPage.SimpleRazorCompiler;

public class CodeNode : Node
{
    public CodeNode(string markup) : base(null, NodeTypeEnum.Top)
    {
        var position = 0;
        position = ReadBody(markup, position);
    }
}
