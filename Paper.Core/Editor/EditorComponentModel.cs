using System.Collections.Generic;

namespace Paper.Core.Editor;



public class EditorComponentModel
{
    public required EditorMember[] Members { get; init; }
    public required EditorComponentModel[] NestedComponents { get; init; }
}
