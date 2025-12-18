interface ScribeResult {
    Trees: ScribeTreeNode[];
    Nodes: Record<string, ScribeNodeData>;
}

interface ScribeTreeNode {
    Id: string;
    ChildNodes: ScribeTreeNode[];
}

interface ScribeNodeData {
    Id: string;
    Value: string[];
    Comment?: ScribeComment;
    Kind: string;
    MetaInfo: MetaInfo;
    ChildNodeIds: string[];
}

interface ScribeComment {
    Comments: string[];
    Guide?: ScribeGuides;
}

interface ScribeGuides {
    I?: string; // Identifier
    L?: number; // Level
    T?: string; // Text
    D?: string; // Description
    P?: string; // Path
    O?: string[]; // OriginIds
    DI?: string[]; // DestinationIds
    Tags?: string[]; // Tags
}

interface MetaInfo {
    ProjectName: string;
    DocumentName: string;
    DocumentPath: string;
    NameSpace: string;
    TypeName: string;
    MemberName: string;
    Identifier: string;
    Line: number;
}

interface ViewConfig {
    activeTreeId: string | null;
    expandedNodeIds: string[]; // List of IDs that are manually expanded
    activeSearchTerm: string;
    tagColors?: Record<string, string>;
}
