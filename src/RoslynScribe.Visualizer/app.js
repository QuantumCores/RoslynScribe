"use strict";
const ICONS = {
    EXPAND: `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>`,
    COLLAPSE: `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line></svg>`,
    DETAILS: `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>`,
    SEARCH: `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>`
};
class ScribeApp {
    constructor() {
        this.data = null;
        this.childToParentMap = new Map();
        this.visibleNodeIds = new Set();
        this.activeTreeId = null;
        this.panZoomInstance = null;
        this.searchResults = [];
        this.currentSearchIndex = -1;
        this.initializeEventListeners();
        mermaid.initialize({
            startOnLoad: false,
            securityLevel: 'loose',
            htmlLabels: true,
            flowchart: {
                useMaxWidth: false,
                htmlLabels: true,
                curve: 'basis'
            }
        });
        window.scribeApp = {
            expandNode: (id) => this.expandNode(id),
            showDetails: (id) => this.showNodeDetails(id)
        };
    }
    initializeEventListeners() {
        document.getElementById('btn-load')?.addEventListener('click', () => {
            document.getElementById('file-input')?.click();
        });
        document.getElementById('file-input')?.addEventListener('change', (e) => {
            const file = e.target.files?.[0];
            if (file)
                this.loadDataFile(file);
        });
        document.getElementById('tree-select')?.addEventListener('change', (e) => {
            const select = e.target;
            this.setActiveTree(select.value);
        });
        document.getElementById('btn-reset')?.addEventListener('click', () => {
            this.resetView();
        });
        const searchInput = document.getElementById('search-input');
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
        document.getElementById('config-input')?.addEventListener('change', (e) => {
            const file = e.target.files?.[0];
            if (file)
                this.loadConfigFile(file);
        });
        document.getElementById('mermaid-output')?.addEventListener('click', (e) => {
            let target = e.target;
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
                }
                target = target.parentElement;
            }
        });
    }
    showLoading(show) {
        const overlay = document.getElementById('loading-overlay');
        if (overlay) {
            overlay.className = show ? 'visible' : '';
        }
        return new Promise(resolve => setTimeout(resolve, 50));
    }
    async loadDataFile(file) {
        await this.showLoading(true);
        try {
            const text = await file.text();
            const json = JSON.parse(text);
            if (!json.Nodes || !json.Trees) {
                throw new Error("Invalid File Format. Missing Nodes or Trees property.");
            }
            this.data = json;
            this.buildChildToParentMap();
            this.populateTreeSelect();
            if (this.data.Trees.length > 0) {
                this.setActiveTree('all');
            }
            else {
                alert("No execution trees found in result.");
            }
            this.enableControls();
        }
        catch (e) {
            alert(e.message || "Error loading file");
            console.error(e);
        }
        finally {
            this.showLoading(false);
        }
    }
    enableControls() {
        const ids = ['tree-select', 'search-input', 'btn-reset', 'btn-save-config', 'btn-load-config'];
        ids.forEach(id => {
            const el = document.getElementById(id);
            if (el)
                el.disabled = false;
        });
        document.getElementById('tree-select').style.display = 'inline-block';
    }
    buildChildToParentMap() {
        this.childToParentMap.clear();
        if (!this.data)
            return;
        const traverse = (node, parentId) => {
            if (parentId) {
                this.childToParentMap.set(node.Id, parentId);
            }
            for (const child of node.ChildNodes) {
                traverse(child, node.Id);
            }
        };
        for (const tree of this.data.Trees) {
            traverse(tree, null);
        }
    }
    populateTreeSelect() {
        const select = document.getElementById('tree-select');
        select.innerHTML = '';
        if (!this.data)
            return;
        const allOption = document.createElement('option');
        allOption.value = 'all';
        allOption.text = 'All Flows';
        select.appendChild(allOption);
        this.data.Trees.forEach((tree, index) => {
            const nodeData = this.data.Nodes[tree.Id];
            const name = nodeData?.MetaInfo?.MemberName || `Flow ${index + 1} (${tree.Id.substring(0, 8)})`;
            const option = document.createElement('option');
            option.value = tree.Id;
            option.text = name;
            select.appendChild(option);
        });
    }
    async setActiveTree(treeId) {
        this.activeTreeId = treeId;
        await this.renderGraph();
    }
    async resetView() {
        this.visibleNodeIds.clear();
        this.searchResults = [];
        this.currentSearchIndex = -1;
        document.getElementById('search-input').value = '';
        await this.renderGraph();
    }
    async expandNode(nodeId) {
        if (!this.data)
            return;
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
            }
        }
    }
    findTreeNode(root, id) {
        if (root.Id === id)
            return root;
        for (const child of root.ChildNodes) {
            const res = this.findTreeNode(child, id);
            if (res)
                return res;
        }
        return null;
    }
    calculateVisibleSet(trees) {
        const visibleSet = new Set();
        const targetNodes = new Set();
        const scan = (node) => {
            const data = this.data.Nodes[node.Id];
            const level = data.Comment?.Guide?.L ?? 0;
            if (level <= 1 || this.visibleNodeIds.has(node.Id)) {
                targetNodes.add(node.Id);
            }
            for (const child of node.ChildNodes) {
                scan(child);
            }
        };
        trees.forEach(t => scan(t));
        targetNodes.forEach(targetId => {
            let curr = targetId;
            while (curr) {
                visibleSet.add(curr);
                const parent = this.childToParentMap.get(curr);
                if (!parent)
                    break;
                curr = parent;
            }
        });
        return visibleSet;
    }
    async renderGraph() {
        await this.showLoading(true);
        const container = document.getElementById('mermaid-output');
        if (!container || !this.data || !this.activeTreeId) {
            this.showLoading(false);
            return;
        }
        const treesToRender = (this.activeTreeId === 'all')
            ? this.data.Trees
            : this.data.Trees.filter(t => t.Id === this.activeTreeId);
        if (treesToRender.length === 0) {
            this.showLoading(false);
            return;
        }
        const nodesToRender = this.calculateVisibleSet(treesToRender);
        let graphDef = "graph TD\n";
        const processedNodes = new Set();
        const generateNode = (treeNode) => {
            const id = treeNode.Id;
            const data = this.data.Nodes[id];
            const guide = data.Comment?.Guide;
            const labelText = (guide?.T || data.Value?.join(' ') || data.MetaInfo?.MemberName || "Unknown").replace(/"/g, "'");
            const cleanLabelText = labelText.replace(/[\n\r]+/g, ' ').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
            let hiddenCount = 0;
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
            const expandBtnHtml = (treeNode.ChildNodes.length > 0 && hiddenCount > 0)
                ? `<button class='node-icon-btn' data-action='expand' data-id='${id}' title='Expand'>${ICONS.EXPAND.replace(/"/g, "'")}</button>`
                : '';
            const detailsBtnHtml = `<button class='node-icon-btn' data-action='details' data-id='${id}' title='Details'>${ICONS.DETAILS.replace(/"/g, "'")}</button>`;
            const htmlLabel = `
                <div class='node-content'>
                    <div class='node-badges'>${badgesHtml}</div>
                    <div class='node-title'>${cleanLabelText}</div>
                    <div class='node-controls'>
                        ${expandBtnHtml}
                        ${detailsBtnHtml}
                    </div>
                </div>
            `.replace(/[\n\r]+/g, '').replace(/\s+/g, ' ');
            const styles = [];
            if (data.Kind === "Document")
                styles.push("node-document");
            if (guide?.Tags) {
                guide.Tags.forEach(tag => {
                    const cleanTag = tag.toLowerCase().replace(/[^a-z0-9]/g, '');
                    styles.push(`tag-${cleanTag}`);
                });
            }
            if (this.searchResults.includes(id)) {
                styles.push("highlighted");
            }
            graphDef += `    ${id}["${htmlLabel}"]\n`;
            if (styles.length > 0) {
                graphDef += `    class ${id} ${styles.join(',')}\n`;
            }
            for (const child of treeNode.ChildNodes) {
                if (nodesToRender.has(child.Id)) {
                    graphDef += `    ${id} --> ${child.Id}\n`;
                    if (!processedNodes.has(child.Id)) {
                        processedNodes.add(child.Id);
                        generateNode(child);
                    }
                }
            }
        };
        treesToRender.forEach(tree => {
            if (!processedNodes.has(tree.Id) && nodesToRender.has(tree.Id)) {
                processedNodes.add(tree.Id);
                generateNode(tree);
            }
        });
        graphDef += `\n    classDef default fill:#e3f2fd,stroke:#333,stroke-width:1px;\n`;
        graphDef += `    classDef highlighted stroke:#ff9800,stroke-width:3px;\n`;
        graphDef += `    classDef tagwarning fill:#fff3e0,stroke:#ffb74d;\n`;
        graphDef += `    classDef tagerror fill:#ffebee,stroke:#ef5350;\n`;
        try {
            const isValid = await mermaid.parse(graphDef);
            if (!isValid)
                throw new Error("Graph parsing failed");
            const { svg } = await mermaid.render('graphDiv', graphDef);
            container.innerHTML = svg;
            const svgEl = container.querySelector('svg');
            if (svgEl) {
                svgEl.setAttribute('width', '100%');
                svgEl.setAttribute('height', '100%');
                svgEl.style.width = '100%';
                svgEl.style.height = '100%';
            }
            this.panZoomInstance = svgPanZoom(container.querySelector('svg'), {
                zoomEnabled: true,
                controlIconsEnabled: true,
                fit: true,
                center: true
            });
        }
        catch (e) {
            console.error("Mermaid Render Error", e);
            container.innerText = "Error rendering graph.";
        }
        finally {
            this.showLoading(false);
        }
    }
    showNodeDetails(id) {
        const data = this.data?.Nodes[id];
        const panel = document.getElementById('side-panel');
        const content = document.getElementById('node-meta-content');
        const commentsSection = document.getElementById('comments-section');
        const commentsContent = document.getElementById('node-comments-content');
        if (!data || !panel || !content)
            return;
        panel.classList.add('open');
        panel.style.display = 'block';
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
            commentsSection.style.display = 'block';
            commentsContent.innerHTML = data.Value.map(v => `<p>${v}</p>`).join('');
        }
        else {
            commentsSection.style.display = 'none';
        }
    }
    async performSearch(term) {
        if (!term || !this.data)
            return;
        term = term.toLowerCase();
        this.searchResults = [];
        this.currentSearchIndex = -1;
        Object.values(this.data.Nodes).forEach(node => {
            const guideText = node.Comment?.Guide?.T?.toLowerCase() || "";
            const rawText = node.Value?.join(' ').toLowerCase() || "";
            const tags = node.Comment?.Guide?.Tags?.map(t => t.toLowerCase()) || [];
            if (guideText.includes(term) || rawText.includes(term) || tags.some(t => t.includes(term))) {
                this.searchResults.push(node.Id);
                this.revealNode(node.Id);
            }
        });
        if (this.searchResults.length > 0) {
            this.currentSearchIndex = 0;
            document.getElementById('btn-next-match').disabled = false;
            document.getElementById('btn-prev-match').disabled = false;
            await this.renderGraph();
            this.focusNode(this.searchResults[0]);
        }
        else {
            alert("No matches found.");
        }
    }
    revealNode(id) {
        let curr = id;
        while (curr) {
            this.visibleNodeIds.add(curr);
            const parent = this.childToParentMap.get(curr);
            if (!parent)
                break;
            curr = parent;
        }
    }
    nextMatch() {
        if (this.searchResults.length === 0)
            return;
        this.currentSearchIndex = (this.currentSearchIndex + 1) % this.searchResults.length;
        this.focusNode(this.searchResults[this.currentSearchIndex]);
    }
    prevMatch() {
        if (this.searchResults.length === 0)
            return;
        this.currentSearchIndex = (this.currentSearchIndex - 1 + this.searchResults.length) % this.searchResults.length;
        this.focusNode(this.searchResults[this.currentSearchIndex]);
    }
    focusNode(id) {
        const el = document.getElementById(id);
        if (el && this.panZoomInstance) {
            const bbox = el.getBBox();
            const sizes = this.panZoomInstance.getSizes();
            const pan = this.panZoomInstance.getPan();
            const zoom = this.panZoomInstance.getZoom();
            const cx = bbox.x + bbox.width / 2;
            const cy = bbox.y + bbox.height / 2;
            const newPanX = (sizes.width / 2) - (cx * zoom);
            const newPanY = (sizes.height / 2) - (cy * zoom);
            this.panZoomInstance.pan({ x: newPanX, y: newPanY });
            this.panZoomInstance.setZoom(1.2);
        }
    }
    saveConfig() {
        const config = {
            activeTreeId: this.activeTreeId,
            expandedNodeIds: Array.from(this.visibleNodeIds),
            activeSearchTerm: document.getElementById('search-input').value
        };
        const blob = new Blob([JSON.stringify(config, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'roslyn-scribe-config.json';
        a.click();
    }
    async loadConfigFile(file) {
        try {
            const text = await file.text();
            const config = JSON.parse(text);
            if (config.activeTreeId)
                this.activeTreeId = config.activeTreeId;
            if (config.expandedNodeIds) {
                config.expandedNodeIds.forEach(id => this.visibleNodeIds.add(id));
            }
            if (config.activeSearchTerm) {
                document.getElementById('search-input').value = config.activeSearchTerm;
            }
            await this.renderGraph();
        }
        catch (e) {
            alert("Error loading config");
        }
    }
}
new ScribeApp();
//# sourceMappingURL=app.js.map