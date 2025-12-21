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
    Guides?: ScribeGuides;
    Kind: string;
    MetaInfo: MetaInfo;
    ChildNodeIds: string[];
}

interface ScribeGuides {
    Id?: string; // Identifier
    Uid?: string; // User defined identifier
    L?: number; // Level
    T?: string; // Text
    D?: string; // Description
    P?: string; // Path
    O?: string[]; // OriginIds
    DUI?: string[]; // DestinationUserIds
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
    expandedNodeLevels?: Record<string, number>; // Node ID to max visible level
    expandedNodeIds?: string[]; // Legacy: list of IDs that are manually expanded
    activeSearchTerm: string;
    tagColors?: Record<string, string>;
}
