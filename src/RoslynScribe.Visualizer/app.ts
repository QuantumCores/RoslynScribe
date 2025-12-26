/// <reference path="types.ts" />

declare var mermaid: any;
declare var svgPanZoom: any;

// SVG Icons
const ICONS = {
    EXPAND: `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>`,
    COLLAPSE: `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line></svg>`,
    DETAILS: `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>`,
    SEARCH: `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>`
};

class ScribeApp {
    private data: ScribeResult | null = null;
    private childToParentMap: Map<string, string> = new Map();
    private expandedNodeMaxLevels: Map<string, number> = new Map();
    private collapsedNodeIds: Set<string> = new Set();
    private activeTreeId: string | null = null;
    private panZoomInstance: any = null;
    private readonly baseVisibleLevel = 1;
    
    // Search State
    private searchResults: string[] = [];
    private currentSearchIndex: number = -1;

    constructor() {
        this.initializeEventListeners();
        
        // Initialize Mermaid
        mermaid.initialize({ 
            startOnLoad: false,
            securityLevel: 'loose', // Required for htmlLabels interactions
            htmlLabels: true,
            flowchart: { 
                useMaxWidth: false, 
                htmlLabels: true,
                curve: 'basis'
            }
        });

        // Expose global methods for HTML onclick events
        (window as any).scribeApp = {
            expandNode: (id: string) => this.expandNode(id),
            showDetails: (id: string) => this.showNodeDetails(id)
        };
    }

    private initializeEventListeners() {
        document.getElementById('btn-load')?.addEventListener('click', () => {
            document.getElementById('file-input')?.click();
        });

        document.getElementById('file-input')?.addEventListener('change', (e: Event) => {
            const file = (e.target as HTMLInputElement).files?.[0];
            if (file) this.loadDataFile(file);
        });

        document.getElementById('tree-select')?.addEventListener('change', (e: Event) => {
            const select = e.target as HTMLSelectElement;
            this.setActiveTree(select.value);
        });

        document.getElementById('btn-reset')?.addEventListener('click', () => {
            this.resetView();
        });

        const searchInput = document.getElementById('search-input') as HTMLInputElement;
        searchInput?.addEventListener('keyup', (e) => {
            if (e.key === 'Enter') {
                this.performSearch(searchInput.value);
            }
        });

        document.getElementById('btn-next-match')?.addEventListener('click', () => this.nextMatch());
        document.getElementById('btn-prev-match')?.addEventListener('click', () => this.prevMatch());

        document.getElementById('btn-save-config')?.addEventListener('click', () => this.saveConfig());
        document.getElementById('btn-load-config')?.addEventListener('click', () => {
            document.getElementById('config-input')?.click();
        });
        document.getElementById('config-input')?.addEventListener('change', (e: Event) => {
            const file = (e.target as HTMLInputElement).files?.[0];
            if (file) this.loadConfigFile(file);
        });
        document.getElementById('btn-close-details')?.addEventListener('click', () => {
            this.hideNodeDetails();
        });

        // Global Event Delegation for Dynamic Mermaid Elements
        // This is robust against parsing errors and scope issues
        document.getElementById('mermaid-output')?.addEventListener('click', (e: Event) => {
            let target = e.target as HTMLElement;
            // Traverse up to find button or relevant container
            while (target && target.id !== 'mermaid-output') {
                if (target.tagName === 'BUTTON' && target.dataset.action) {
                    const action = target.dataset.action;
                    const id = target.dataset.id;
                    if (action === 'expand' && id) {
                        this.expandNode(id);
                        e.stopPropagation();
                        return;
                    }
                    if (action === 'details' && id) {
                        this.showNodeDetails(id);
                        e.stopPropagation();
                        return;
                    }
                    if (action === 'collapse' && id) {
                        this.retractNode(id);
                        e.stopPropagation();
                        return;
                    }
                }
                target = target.parentElement as HTMLElement;
            }
        });
    }

    private showLoading(show: boolean) {
        const overlay = document.getElementById('loading-overlay');
        if (overlay) {
            overlay.className = show ? 'visible' : '';
        }
        // Force layout update to ensure spinner renders before heavy JS
        return new Promise(resolve => setTimeout(resolve, 50));
    }

    private async loadDataFile(file: File) {
        await this.showLoading(true);
        try {
            const text = await file.text();
            const json = JSON.parse(text);

            // Schema Validation
            if (!json.Nodes || !json.Trees) {
                throw new Error("Invalid File Format. Missing Nodes or Trees property.");
            }

            this.data = json as ScribeResult;
            this.buildChildToParentMap();
            this.populateTreeSelect();
            
            // Default: Select 'all'
            if (this.data.Trees.length > 0) {
                this.setActiveTree('all');
            } else {
                alert("No execution trees found in result.");
            }

            this.enableControls();

        } catch (e: any) {
            alert(e.message || "Error loading file");
            console.error(e);
        } finally {
            this.showLoading(false);
        }
    }

    private enableControls() {
        const ids = ['tree-select', 'search-input', 'btn-reset', 'btn-save-config', 'btn-load-config'];
        ids.forEach(id => {
            const el = document.getElementById(id);
            if (el) (el as any).disabled = false;
        });
        document.getElementById('tree-select')!.style.display = 'inline-block';
    }

    private buildChildToParentMap() {
        this.childToParentMap.clear();
        if (!this.data) return;

        const traverse = (node: ScribeTreeNode, parentId: string | null) => {
            if (parentId) {
                this.childToParentMap.set(node.Id, parentId);
            }
            // ScribeTreeNode doesn't have child GUIDs directly, only recursive objects.
            // But ScribeNodeData has ChildNodeIds. 
            // The Tree structure is authoritative for the visual hierarchy.
            for (const child of node.ChildNodes) {
                traverse(child, node.Id);
            }
        };

        for (const tree of this.data.Trees) {
            traverse(tree, null);
        }
    }

    private populateTreeSelect() {
        const select = document.getElementById('tree-select') as HTMLSelectElement;
        select.innerHTML = '';
        
        if (!this.data) return;

        // Add "All Flows" option
        const allOption = document.createElement('option');
        allOption.value = 'all';
        allOption.text = 'All Flows';
        select.appendChild(allOption);

        this.data.Trees.forEach((tree, index) => {
            const nodeData = this.data!.Nodes[tree.Id];
            const name = nodeData?.MetaInfo?.MemberName || `Flow ${index + 1} (${tree.Id.substring(0, 8)})`;
            const option = document.createElement('option');
            option.value = tree.Id;
            option.text = name;
            select.appendChild(option);
        });
    }

    private async setActiveTree(treeId: string) {
        this.activeTreeId = treeId;
        // this.expandedNodeMaxLevels.clear(); // Keep expansions when switching views?
        await this.renderGraph();
    }

    private async resetView() {
        this.expandedNodeMaxLevels.clear();
        this.collapsedNodeIds.clear();
        this.searchResults = [];
        this.currentSearchIndex = -1;
        (document.getElementById('search-input') as HTMLInputElement).value = '';
        await this.renderGraph();
    }

    public async expandNode(nodeId: string) {
        if (!this.data) return;
        this.clearCollapsedAncestors(nodeId);
        const currentMax = this.getCurrentAllowedMax(nodeId);
        this.expandedNodeMaxLevels.set(nodeId, currentMax + 1);
        await this.renderGraph();
    }

    public async retractNode(nodeId: string) {
        if (!this.data) return;
        this.collapsedNodeIds.add(nodeId);
        await this.renderGraph();
    }

    private findTreeNode(root: ScribeTreeNode, id: string): ScribeTreeNode | null {
        if (root.Id === id) return root;
        for (const child of root.ChildNodes) {
            const res = this.findTreeNode(child, id);
            if (res) return res;
        }
        return null;
    }

    private getNodeLevel(nodeId: string): number {
        const level = this.data?.Nodes[nodeId]?.Guides?.L;
        return typeof level === 'number' ? level : 0;
    }

    private clearCollapsedAncestors(nodeId: string) {
        let curr: string | undefined = nodeId;
        while (curr) {
            this.collapsedNodeIds.delete(curr);
            const parent = this.childToParentMap.get(curr);
            if (!parent) break;
            curr = parent;
        }
    }

    private getCurrentAllowedMax(nodeId: string): number {
        let maxLevel = this.baseVisibleLevel;
        let curr: string | undefined = nodeId;

        while (curr) {
            const expandedMax = this.expandedNodeMaxLevels.get(curr);
            if (expandedMax !== undefined && expandedMax > maxLevel) {
                maxLevel = expandedMax;
            }
            const parent = this.childToParentMap.get(curr);
            if (!parent) break;
            curr = parent;
        }

        return maxLevel;
    }

    private computeVisibleSet(trees: ScribeTreeNode[]): Set<string> {
        const visibleSet = new Set<string>();

        const traverse = (node: ScribeTreeNode, allowedMax: number, collapseActive: boolean) => {
            const nodeId = node.Id;
            const level = this.getNodeLevel(nodeId);
            const isCollapsed = collapseActive || this.collapsedNodeIds.has(nodeId);
            const effectiveAllowedMax = isCollapsed ? this.baseVisibleLevel : allowedMax;
            const isVisible = level <= effectiveAllowedMax;

            if (isVisible) {
                visibleSet.add(nodeId);
            }

            let nextAllowedMax = effectiveAllowedMax;
            if (!isCollapsed) {
                const expandedMax = this.expandedNodeMaxLevels.get(nodeId);
                if (expandedMax !== undefined && expandedMax > nextAllowedMax) {
                    nextAllowedMax = expandedMax;
                }
            }
            for (const child of node.ChildNodes) {
                traverse(child, nextAllowedMax, isCollapsed);
            }
        };

        trees.forEach(tree => traverse(tree, this.baseVisibleLevel, false));

        return visibleSet;
    }

    private computeEdges(trees: ScribeTreeNode[], visibleSet: Set<string>): Array<{ from: string; to: string }> {
        const edges: Array<{ from: string; to: string }> = [];

        const traverse = (node: ScribeTreeNode, nearestVisibleAncestor: string | null, collapseActive: boolean) => {
            const nodeId = node.Id;
            const isCollapsed = collapseActive || this.collapsedNodeIds.has(nodeId);
            const suppressForCollapse = collapseActive && this.getNodeLevel(nodeId) > this.baseVisibleLevel;
            const isVisibleHere = visibleSet.has(nodeId) && !suppressForCollapse;

            if (isVisibleHere) {
                if (nearestVisibleAncestor) {
                    edges.push({ from: nearestVisibleAncestor, to: nodeId });
                }
                nearestVisibleAncestor = nodeId;
            }

            for (const child of node.ChildNodes) {
                traverse(child, nearestVisibleAncestor, isCollapsed);
            }
        };

        trees.forEach(tree => traverse(tree, null, false));

        return edges;
    }

    private async renderGraph() {
        await this.showLoading(true);
        const container = document.getElementById('mermaid-output');
        if (!container || !this.data || !this.activeTreeId) {
            this.showLoading(false);
            return;
        }

        // 1. Build Graph Definition
        // If activeTreeId is 'all', render all trees. Otherwise render specific one.
        const treesToRender = (this.activeTreeId === 'all') 
            ? this.data.Trees 
            : this.data.Trees.filter(t => t.Id === this.activeTreeId);

        if (treesToRender.length === 0) {
            this.showLoading(false);
            return;
        }

        // Calculate Visibility
        const nodesToRender = this.computeVisibleSet(treesToRender);
        const edges = this.computeEdges(treesToRender, nodesToRender);

        let graphDef = "graph TD\n";
        
        // We need to track processed nodes to handle DAG/Deduplication visually
        const processedNodes = new Set<string>();
        
        // Helper to generate node string
        const generateNodeDefinition = (treeNode: ScribeTreeNode) => {
            const id = treeNode.Id;
            const data = this.data!.Nodes[id];
            const guide = data.Guides;
            const nodeLevel = this.getNodeLevel(id);
            const directHigherChildren = treeNode.ChildNodes.filter(child => this.getNodeLevel(child.Id) > nodeLevel);
            const hasDirectHigherHidden = directHigherChildren.some(child => !nodesToRender.has(child.Id));
            const hasDirectHigherVisible = directHigherChildren.some(child => nodesToRender.has(child.Id));
            
            // Build Node Content
            const labelText = (guide?.T || data.MetaInfo?.MemberName || "Unknown").replace(/"/g, "'");
            const cleanLabelText = labelText.replace(/[\n\r]+/g, ' ').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
            
            // Badges (Hidden Children Count)
            // We need to check if children are NOT visible
            let hiddenCount = 0;
            // Check actual children in the Tree structure, not just Data
            if (treeNode.ChildNodes) {
                for (const child of treeNode.ChildNodes) {
                    if (!nodesToRender.has(child.Id)) {
                        hiddenCount++;
                    }
                }
            }

            const badgesHtml = hiddenCount > 0 
                ? `<div class='badge'>+${hiddenCount}</div>` 
                : '';
                
            // Use data attributes for event delegation
            const canExpand = hasDirectHigherHidden;
            const expandBtnHtml = canExpand
                ? `<button class='node-icon-btn' data-action='expand' data-id='${id}' title='Expand'>${ICONS.EXPAND.replace(/"/g, "'")}</button>`
                : '';

            const canRetract = hasDirectHigherVisible;
            const collapseBtnHtml = canRetract
                ? `<button class='node-icon-btn' data-action='collapse' data-id='${id}' title='Retract'>${ICONS.COLLAPSE.replace(/"/g, "'")}</button>`
                : '';

            const detailsBtnHtml = `<button class='node-icon-btn' data-action='details' data-id='${id}' title='Details'>${ICONS.DETAILS.replace(/"/g, "'")}</button>`;


            const htmlLabel = `
                <div class='node-content'>
                    <div class='node-badges'>${badgesHtml}</div>
                    <div class='node-title'>${cleanLabelText}</div>
                    <div class='node-controls'>
                        ${expandBtnHtml}
                        ${collapseBtnHtml}
                        ${detailsBtnHtml}
                    </div>
                </div>
            `.replace(/[\n\r]+/g, '').replace(/\s+/g, ' ');
            
            // Style Classes
            const styles: string[] = [];
            // Semantic colors
            if (data.Kind === "Document") styles.push("node-document");
            // Tags
            if (guide?.Tags) {
                guide.Tags.forEach(tag => {
                    const cleanTag = tag.toLowerCase().replace(/[^a-z0-9]/g, '');
                    styles.push(`tag-${cleanTag}`);
                });
            }
            if (this.searchResults.includes(id)) {
                styles.push("highlighted");
            }

            // Emit Node Definition
            // id["label"]
            graphDef += `    ${id}["${htmlLabel}"]\n`;
            
            if (styles.length > 0) {
                // Mermaid classDef is global usually, or we use `class id className`
                // We define classes at the end or use style?
                // `class ID classname`
                graphDef += `    class ${id} ${styles.join(',')}\n`;
            }
            
            // Traverse Children
        };

        const traverseNodes = (treeNode: ScribeTreeNode) => {
            if (nodesToRender.has(treeNode.Id) && !processedNodes.has(treeNode.Id)) {
                processedNodes.add(treeNode.Id);
                generateNodeDefinition(treeNode);
            }
            for (const child of treeNode.ChildNodes) {
                traverseNodes(child);
            }
        };

        treesToRender.forEach(tree => {
            traverseNodes(tree);
        });

        const edgeSet = new Set<string>();
        for (const edge of edges) {
            const key = `${edge.from}-->${edge.to}`;
            if (!edgeSet.has(key)) {
                edgeSet.add(key);
                graphDef += `    ${edge.from} --> ${edge.to}\n`;
            }
        }

        // Add Style Definitions (Dynamic)
        graphDef += `\n    classDef default fill:#e3f2fd,stroke:#333,stroke-width:1px;\n`;
        graphDef += `    classDef highlighted stroke:#ff9800,stroke-width:3px;\n`;
        graphDef += `    classDef tagwarning fill:#fff3e0,stroke:#ffb74d;\n`;
        graphDef += `    classDef tagerror fill:#ffebee,stroke:#ef5350;\n`;

        // Render
        try {
            // Check if graph is valid
            // console.log(graphDef);
            const isValid = await mermaid.parse(graphDef);
            if (!isValid) throw new Error("Graph parsing failed");
            
            // We use mermaid.render (async in v10)
            const { svg } = await mermaid.render('graphDiv', graphDef);
            container.innerHTML = svg;
            
            // Fix svg size to take full container
            const svgEl = container.querySelector('svg');
            if(svgEl) {
                svgEl.setAttribute('width', '100%');
                svgEl.setAttribute('height', '100%');
                svgEl.style.width = '100%';
                svgEl.style.height = '100%';
            }

            // Initialize PanZoom
            this.panZoomInstance = svgPanZoom(container.querySelector('svg'), {
                zoomEnabled: true,
                controlIconsEnabled: true,
                fit: true,
                center: true
            });

        } catch (e) {
            console.error("Mermaid Render Error", e);
            container.innerText = "Error rendering graph.";
        } finally {
            this.showLoading(false);
        }
    }

    private renderMetaRow(label: string, value: string | number | null | undefined): string {
        if (value === undefined || value === null || value === '') return '';
        return `<div class="meta-item"><span class="meta-label">${label}</span><span class="meta-value">${value}</span></div>`;
    }

    private hideNodeDetails() {
        const panel = document.getElementById('side-panel');
        if (!panel) return;
        panel.classList.remove('open');
        panel.style.display = 'none';
    }

    public showNodeDetails(id: string) {
        const data = this.data?.Nodes[id];
        const panel = document.getElementById('side-panel');
        const content = document.getElementById('node-meta-content');
        const commentsSection = document.getElementById('comments-section');
        const commentsContent = document.getElementById('node-comments-content');

        if (!data || !panel || !content) return;

        panel.classList.add('open');
        // Ensure display block via style if class isn't enough (though css handles it)
        panel.style.display = 'block';
        
        const meta = data.MetaInfo;
        const guide = data.Guides;
        const tags = guide?.Tags?.length ? guide.Tags.join(', ') : '';
        const rows = [
            this.renderMetaRow('ID', data.Id),
            this.renderMetaRow('Kind', data.Kind),
            this.renderMetaRow('Level', guide?.L),
            this.renderMetaRow('Identifier', guide?.Id),
            this.renderMetaRow('User Id', guide?.Uid),
            this.renderMetaRow('Text', guide?.T),
            this.renderMetaRow('Description', guide?.D),
            this.renderMetaRow('Path', guide?.P),
            this.renderMetaRow('Tags', tags),
            this.renderMetaRow('Project', meta.ProjectName),
            this.renderMetaRow('Namespace', meta.NameSpace),
            this.renderMetaRow('Class', meta.TypeName),
            this.renderMetaRow('Member', meta.MemberName),
            this.renderMetaRow('Identifier (Meta)', meta.Identifier),
            this.renderMetaRow('Document', meta.DocumentName),
            this.renderMetaRow('Document Path', meta.DocumentPath),
            this.renderMetaRow('Line', meta.Line),
        ];
        content.innerHTML = rows.filter(row => row.length > 0).join('');

        if (guide?.D) {
            commentsSection!.style.display = 'block';
            commentsContent!.innerHTML = `<p>${guide.D}</p>`;
        } else {
            commentsSection!.style.display = 'none';
        }
    }

    // Search
    private async performSearch(term: string) {
        if (!term || !this.data) return;
        term = term.toLowerCase();

        this.searchResults = [];
        this.currentSearchIndex = -1;

        // Find matching nodes
        Object.values(this.data.Nodes).forEach(node => {
            const guideText = node.Guides?.T?.toLowerCase() || "";
            const tags = node.Guides?.Tags?.map(t => t.toLowerCase()) || [];
            
            if (guideText.includes(term) || tags.some(t => t.includes(term))) {
                // Match found
                this.searchResults.push(node.Id);
                // Reveal path
                this.revealNode(node.Id);
            }
        });

        if (this.searchResults.length > 0) {
            this.currentSearchIndex = 0;
            // Enable Next/Prev
            (document.getElementById('btn-next-match') as HTMLButtonElement).disabled = false;
            (document.getElementById('btn-prev-match') as HTMLButtonElement).disabled = false;
            
            await this.renderGraph();
            this.focusNode(this.searchResults[0]);
        } else {
            alert("No matches found.");
        }
    }

    private revealNode(id: string) {
        const targetLevel = this.getNodeLevel(id);
        if (targetLevel <= this.baseVisibleLevel) return;

        let curr = id;
        while (curr) {
            const existing = this.expandedNodeMaxLevels.get(curr) ?? this.baseVisibleLevel;
            if (targetLevel > existing) {
                this.expandedNodeMaxLevels.set(curr, targetLevel);
            }
            const parent = this.childToParentMap.get(curr);
            if (!parent) break;
            curr = parent;
        }
    }

    private nextMatch() {
        if (this.searchResults.length === 0) return;
        this.currentSearchIndex = (this.currentSearchIndex + 1) % this.searchResults.length;
        this.focusNode(this.searchResults[this.currentSearchIndex]);
    }

    private prevMatch() {
        if (this.searchResults.length === 0) return;
        this.currentSearchIndex = (this.currentSearchIndex - 1 + this.searchResults.length) % this.searchResults.length;
        this.focusNode(this.searchResults[this.currentSearchIndex]);
    }

    private focusNode(id: string) {
        // Use svg-pan-zoom to center
        // Needs finding the SVG element for the node
        const el = document.getElementById(id); // Mermaid uses ID as ID
        if (el && this.panZoomInstance) {
            // Calculate center
            // This is tricky with svg-pan-zoom API directly on element, 
            // usually we need bbox.
            // Simplified:
            // this.panZoomInstance.zoomAtPoint(2, {x: ..., y: ...});
            // Let's just highlight for now. CSS handles highlight.
            // To actually pan:
            const bbox = (el as any).getBBox();
            const sizes = this.panZoomInstance.getSizes();
            
            // Pan to center of bbox
            // Current pan
            const pan = this.panZoomInstance.getPan();
            const zoom = this.panZoomInstance.getZoom();
            
            // Target center in SVG coords
            const cx = bbox.x + bbox.width / 2;
            const cy = bbox.y + bbox.height / 2;
            
            // Target center in Screen coords
            // screenX = (svgX * zoom) + panX
            // We want screenX = sizes.width / 2
            
            const newPanX = (sizes.width / 2) - (cx * zoom);
            const newPanY = (sizes.height / 2) - (cy * zoom);
            
            this.panZoomInstance.pan({x: newPanX, y: newPanY});
            this.panZoomInstance.setZoom(1.2); // slight zoom in
        }
    }

    private saveConfig() {
        const expandedNodeLevels: Record<string, number> = {};
        this.expandedNodeMaxLevels.forEach((value, key) => {
            expandedNodeLevels[key] = value;
        });

        const config: ViewConfig = {
            activeTreeId: this.activeTreeId,
            expandedNodeLevels,
            collapsedNodeIds: Array.from(this.collapsedNodeIds),
            activeSearchTerm: (document.getElementById('search-input') as HTMLInputElement).value
        };
        const blob = new Blob([JSON.stringify(config, null, 2)], {type: 'application/json'});
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'roslyn-scribe-config.json';
        a.click();
    }

    private async loadConfigFile(file: File) {
        try {
            const text = await file.text();
            const config = JSON.parse(text) as ViewConfig;
            
            if (config.activeTreeId) this.activeTreeId = config.activeTreeId;
            this.expandedNodeMaxLevels.clear();
            if (config.expandedNodeLevels) {
                Object.entries(config.expandedNodeLevels).forEach(([id, level]) => {
                    if (typeof level === 'number') {
                        this.expandedNodeMaxLevels.set(id, level);
                    }
                });
            } else if (config.expandedNodeIds) {
                config.expandedNodeIds.forEach(id => {
                    const current = this.getCurrentAllowedMax(id);
                    this.expandedNodeMaxLevels.set(id, current + 1);
                });
            }
            this.collapsedNodeIds.clear();
            if (config.collapsedNodeIds) {
                config.collapsedNodeIds.forEach(id => this.collapsedNodeIds.add(id));
            }
            if (config.activeSearchTerm) {
                (document.getElementById('search-input') as HTMLInputElement).value = config.activeSearchTerm;
                // Maybe trigger search?
            }
            
            await this.renderGraph();
        } catch (e) {
            alert("Error loading config");
        }
    }
}

// Bootstrap
new ScribeApp();
