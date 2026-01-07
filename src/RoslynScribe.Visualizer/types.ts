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
    OUI?: string[]; // OriginUserIds
    DUI?: string[]; // DestinationUserIds
    Tags?: string[]; // Tags
}

interface MetaInfo {
    SolutionName: string;
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
    collapsedNodeIds?: string[]; // Node IDs with retracted higher-level nodes
    subgraphMode?: 'project' | 'folder' | 'none';
    subgraphColors?: {
        project?: Record<string, string>;
        folder?: Record<string, string>;
    };
    activeSearchTerm: string;
    tagColors?: Record<string, string>;
}
