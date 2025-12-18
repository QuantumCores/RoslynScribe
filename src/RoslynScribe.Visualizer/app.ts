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
    private visibleNodeIds: Set<string> = new Set();
    private activeTreeId: string | null = null;
    private panZoomInstance: any = null;
    
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
            
            // Default: Select first tree
            if (this.data.Trees.length > 0) {
                this.setActiveTree(this.data.Trees[0].Id);
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
        this.visibleNodeIds.clear(); // Reset expansions on tree switch? Or keep? Usually reset is cleaner.
        // Or keep Level 1 rule.
        await this.renderGraph();
    }

    private async resetView() {
        this.visibleNodeIds.clear();
        this.searchResults = [];
        this.currentSearchIndex = -1;
        (document.getElementById('search-input') as HTMLInputElement).value = '';
        await this.renderGraph();
    }

    public async expandNode(nodeId: string) {
        if (!this.data) return;
        
        // Add specific children to visible set
        // We need to find the TreeNode for this ID to get its children
        // Since we don't have a direct ID->TreeNode map (only ID->NodeData), we might need to scan the current tree or map it.
        // Actually, NodeData has ChildNodeIds, which matches the TreeNode structure usually.
        // Let's rely on NodeData for finding children IDs to reveal.
        
        const nodeData = this.data.Nodes[nodeId];
        if (nodeData && nodeData.ChildNodeIds) {
            let hasChanges = false;
            for (const childId of nodeData.ChildNodeIds) {
                if (!this.visibleNodeIds.has(childId)) {
                    this.visibleNodeIds.add(childId);
                    hasChanges = true;
                }
            }
            if (hasChanges) {
                await this.renderGraph();
                // Restore zoom? Usually re-render resets zoom unless we explicitly save/restore.
                // pan-zoom library has getZoom/getPan.
            }
        }
    }

    private findTreeNode(root: ScribeTreeNode, id: string): ScribeTreeNode | null {
        if (root.Id === id) return root;
        for (const child of root.ChildNodes) {
            const res = this.findTreeNode(child, id);
            if (res) return res;
        }
        return null;
    }

    private async renderGraph() {
        await this.showLoading(true);
        const container = document.getElementById('mermaid-output');
        if (!container || !this.data || !this.activeTreeId) {
            this.showLoading(false);
            return;
        }

        // 1. Build Graph Definition
        const activeTree = this.data.Trees.find(t => t.Id === this.activeTreeId);
        if (!activeTree) return;

        let graphDef = "graph TD\n";
        
        // We need to track processed nodes to handle DAG/Deduplication visually
        const processedNodes = new Set<string>();
        
        // Helper to generate node string
        const generateNode = (treeNode: ScribeTreeNode) => {
            const id = treeNode.Id;
            const data = this.data!.Nodes[id];
            
            // Visibility Check
            // Rule: Visible IF (Level <= 1) OR (In visibleNodeIds)
            // AND Parent must be effectively processed (we are in recursion, so parent called us)
            // But wait, if we are here, parent decided to process us? 
            // No, we need to decide if WE should continue to children.
            
            const guide = data.Comment?.Guide;
            const level = guide?.L ?? 0; // Default to 0? Or 1?
            
            // NOTE: PRD says "Default State: Level = 1". So Level 1 is visible. Level 2 is hidden.
            // If Level is undefined, treat as 1 (visible) or 0? 
            // If Guide is null, it's just a node.
            
            // Is this node visible?
            // Root is always visible.
            // Others depend on logic.
            // Actually, we should just traverse. If a node is visible, we emit it.
            // If it has hidden children, we emit a badge.
            // If it is NOT visible (e.g. Level 2 and not expanded), we stop recursion.
            
            // Wait, "Level 1" means nodes with L=1. What about L=0?
            // Let's assume L <= 1 is visible by default.
            
            const isVisibleByDefault = (level !== undefined && level <= 1) || (level === undefined);
            const isExplicitlyVisible = this.visibleNodeIds.has(id);
            const isVisible = isVisibleByDefault || isExplicitlyVisible;
            
            // However, if parent is hidden, child shouldn't be rendered (unless graph is disconnected).
            // Since we traverse from root, if we stop at parent, child is never reached. Good.
            
            // Build Node Content
            const labelText = (guide?.T || data.Value?.join(' ') || data.MetaInfo?.MemberName || "Unknown").replace(/"/g, "'");
            const cleanLabelText = labelText.replace(/[\n\r]+/g, ' ').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
            
            // Badges (Hidden Children Count)
            // We need to check if children are NOT visible
            let hiddenCount = 0;
            // Check actual children in the Tree structure, not just Data
            if (treeNode.ChildNodes) {
                for (const child of treeNode.ChildNodes) {
                    const childData = this.data!.Nodes[child.Id];
                    const childLevel = childData.Comment?.Guide?.L ?? 0;
                    const childDefaultVis = (childLevel <= 1);
                    const childExplicitVis = this.visibleNodeIds.has(child.Id);
                    if (!childDefaultVis && !childExplicitVis) {
                        hiddenCount++;
                    }
                }
            }

            const badgesHtml = hiddenCount > 0 
                ? `<div class='badge'>+${hiddenCount}</div>` 
                : '';
                
            const expandBtnHtml = (treeNode.ChildNodes.length > 0 && hiddenCount > 0)
                ? `<button class='node-icon-btn' onclick='window.scribeApp.expandNode("${id}")' title='Expand'>${ICONS.EXPAND.replace(/"/g, "'")}</button>`
                : '';

            const detailsBtnHtml = `<button class='node-icon-btn' onclick='window.scribeApp.showDetails("${id}")' title='Details'>${ICONS.DETAILS.replace(/"/g, "'")}</button>`;


            const htmlLabel = `
                <div class='node-content'>
                    <div class='node-badges'>${badgesHtml}</div>
                    <div class='node-title'>${cleanLabelText}</div>
                    <div class='node-controls'>
                        ${expandBtnHtml}
                        ${detailsBtnHtml}
                    </div>
                </div>
            `;
            
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
            for (const child of treeNode.ChildNodes) {
                const childData = this.data!.Nodes[child.Id];
                const childLevel = childData.Comment?.Guide?.L ?? 0;
                const childIsVisible = (childLevel <= 1) || this.visibleNodeIds.has(child.Id);
                
                if (childIsVisible) {
                    // Emit Edge
                    // Check for recursion (if child was already visited in this path? No, mermaid handles DAG)
                    // But we might want styled edges for loopbacks.
                    // Simple edge for now:
                    graphDef += `    ${id} --> ${child.Id}\n`;
                    
                    if (!processedNodes.has(child.Id)) {
                        processedNodes.add(child.Id);
                        generateNode(child);
                    }
                }
            }
        };

        processedNodes.add(activeTree.Id);
        generateNode(activeTree);

        // Add Style Definitions (Dynamic)
        // We can pre-define common ones or generate.
        graphDef += `\n    classDef default fill:#e3f2fd,stroke:#333,stroke-width:1px;\n`;
        graphDef += `    classDef highlighted stroke:#ff9800,stroke-width:3px;\n`;
        graphDef += `    classDef tagwarning fill:#fff3e0,stroke:#ffb74d;\n`;
        graphDef += `    classDef tagerror fill:#ffebee,stroke:#ef5350;\n`;

        // Render
        try {
            // Check if graph is valid
            const isValid = await mermaid.parse(graphDef);
            if (!isValid) throw new Error("Graph parsing failed");
            
            // We use mermaid.render (async in v10)
            const { svg } = await mermaid.render('graphDiv', graphDef);
            container.innerHTML = svg;
            
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

    public showNodeDetails(id: string) {
        const data = this.data?.Nodes[id];
        const panel = document.getElementById('side-panel');
        const content = document.getElementById('node-meta-content');
        const commentsSection = document.getElementById('comments-section');
        const commentsContent = document.getElementById('node-comments-content');

        if (!data || !panel || !content) return;

        panel.classList.add('open');
        
        const meta = data.MetaInfo;
        content.innerHTML = `
            <div class="meta-item"><span class="meta-label">ID</span><span class="meta-value">${data.Id}</span></div>
            <div class="meta-item"><span class="meta-label">Project</span><span class="meta-value">${meta.ProjectName}</span></div>
            <div class="meta-item"><span class="meta-label">Namespace</span><span class="meta-value">${meta.NameSpace}</span></div>
            <div class="meta-item"><span class="meta-label">Class</span><span class="meta-value">${meta.TypeName}</span></div>
            <div class="meta-item"><span class="meta-label">Member</span><span class="meta-value">${meta.MemberName}</span></div>
            <div class="meta-item"><span class="meta-label">File</span><span class="meta-value">${meta.DocumentName}:${meta.Line}</span></div>
            <div class="meta-item"><span class="meta-label">Kind</span><span class="meta-value">${data.Kind}</span></div>
        `;

        if (data.Value && data.Value.length > 0) {
            commentsSection!.style.display = 'block';
            commentsContent!.innerHTML = data.Value.map(v => `<p>${v}</p>`).join('');
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
            const guideText = node.Comment?.Guide?.T?.toLowerCase() || "";
            const rawText = node.Value?.join(' ').toLowerCase() || "";
            const tags = node.Comment?.Guide?.Tags?.map(t => t.toLowerCase()) || [];
            
            if (guideText.includes(term) || rawText.includes(term) || tags.some(t => t.includes(term))) {
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
        // Walk up parents and add to visible set
        let curr = id;
        while(curr) {
            this.visibleNodeIds.add(curr);
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
        const config: ViewConfig = {
            activeTreeId: this.activeTreeId,
            expandedNodeIds: Array.from(this.visibleNodeIds),
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
            if (config.expandedNodeIds) {
                config.expandedNodeIds.forEach(id => this.visibleNodeIds.add(id));
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

