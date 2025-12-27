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
        this.expandedNodeMaxLevels = new Map();
        this.collapsedNodeIds = new Set();
        this.activeTreeId = null;
        this.panZoomInstance = null;
        this.baseVisibleLevel = 1;
        this.subgraphMode = 'project';
        this.subgraphColors = {
            project: {},
            folder: {}
        };
        this.subgraphPalette = ['#e8f5e9', '#e3f2fd', '#fff3e0', '#f3e5f5', '#e0f7fa', '#fce4ec'];
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
        document.getElementById('btn-edit-config')?.addEventListener('click', () => {
            this.openConfigModal();
        });
        document.getElementById('btn-config-close')?.addEventListener('click', () => {
            this.closeConfigModal();
        });
        document.getElementById('btn-config-apply')?.addEventListener('click', () => {
            this.applyConfigModal();
        });
        document.getElementById('subgraph-mode-select')?.addEventListener('change', (e) => {
            const select = e.target;
            this.renderSubgraphColorList(select.value);
        });
        document.getElementById('btn-close-details')?.addEventListener('click', () => {
            this.hideNodeDetails();
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
                    if (action === 'collapse' && id) {
                        this.retractNode(id);
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
        const ids = ['tree-select', 'search-input', 'btn-reset', 'btn-save-config', 'btn-load-config', 'btn-edit-config'];
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
        this.expandedNodeMaxLevels.clear();
        this.collapsedNodeIds.clear();
        this.searchResults = [];
        this.currentSearchIndex = -1;
        document.getElementById('search-input').value = '';
        await this.renderGraph();
    }
    async expandNode(nodeId) {
        if (!this.data)
            return;
        this.clearCollapsedAncestors(nodeId);
        const currentMax = this.getCurrentAllowedMax(nodeId);
        this.expandedNodeMaxLevels.set(nodeId, currentMax + 1);
        await this.renderGraph();
    }
    async retractNode(nodeId) {
        if (!this.data)
            return;
        this.collapsedNodeIds.add(nodeId);
        await this.renderGraph();
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
    getNodeLevel(nodeId) {
        const level = this.data?.Nodes[nodeId]?.Guides?.L;
        return typeof level === 'number' ? level : 0;
    }
    clearCollapsedAncestors(nodeId) {
        let curr = nodeId;
        while (curr) {
            this.collapsedNodeIds.delete(curr);
            const parent = this.childToParentMap.get(curr);
            if (!parent)
                break;
            curr = parent;
        }
    }
    getCurrentAllowedMax(nodeId) {
        let maxLevel = this.baseVisibleLevel;
        let curr = nodeId;
        while (curr) {
            const expandedMax = this.expandedNodeMaxLevels.get(curr);
            if (expandedMax !== undefined && expandedMax > maxLevel) {
                maxLevel = expandedMax;
            }
            const parent = this.childToParentMap.get(curr);
            if (!parent)
                break;
            curr = parent;
        }
        return maxLevel;
    }
    openConfigModal() {
        if (!this.data)
            return;
        const modal = document.getElementById('config-modal');
        const select = document.getElementById('subgraph-mode-select');
        if (!modal || !select)
            return;
        select.value = this.subgraphMode;
        this.renderSubgraphColorList(this.subgraphMode);
        modal.classList.add('open');
    }
    closeConfigModal() {
        const modal = document.getElementById('config-modal');
        if (!modal)
            return;
        modal.classList.remove('open');
    }
    applyConfigModal() {
        const select = document.getElementById('subgraph-mode-select');
        if (!select)
            return;
        const mode = select.value;
        this.subgraphMode = mode;
        this.applySubgraphColorsFromModal(mode);
        this.closeConfigModal();
        this.renderGraph();
    }
    applySubgraphColorsFromModal(mode) {
        if (mode === 'none')
            return;
        const list = document.getElementById('subgraph-colors-list');
        if (!list)
            return;
        const inputs = Array.from(list.querySelectorAll('input[type="color"]'));
        const map = { ...(this.subgraphColors[mode] || {}) };
        inputs.forEach(input => {
            const group = input.dataset.group;
            if (group) {
                map[group] = input.value;
            }
        });
        this.subgraphColors[mode] = map;
    }
    renderSubgraphColorList(mode) {
        const list = document.getElementById('subgraph-colors-list');
        if (!list)
            return;
        list.innerHTML = '';
        if (mode === 'none') {
            const empty = document.createElement('p');
            empty.className = 'modal-note';
            empty.textContent = 'Subgraphs are disabled.';
            list.appendChild(empty);
            return;
        }
        const groups = this.getSubgraphGroups(mode);
        if (groups.length === 0) {
            const empty = document.createElement('p');
            empty.className = 'modal-note';
            empty.textContent = 'Load data to edit subgraph colors.';
            list.appendChild(empty);
            return;
        }
        groups.forEach((group, index) => {
            const row = document.createElement('div');
            row.className = 'color-row';
            const label = document.createElement('label');
            const input = document.createElement('input');
            const inputId = `subgraph-color-${mode}-${index}`;
            label.setAttribute('for', inputId);
            label.textContent = group;
            input.type = 'color';
            input.id = inputId;
            input.dataset.group = group;
            input.value = this.subgraphColors[mode]?.[group] ?? this.getDefaultSubgraphColor(group);
            row.appendChild(label);
            row.appendChild(input);
            list.appendChild(row);
        });
    }
    getSubgraphGroups(mode) {
        if (!this.data || mode === 'none')
            return [];
        const treesToRender = (this.activeTreeId === 'all')
            ? this.data.Trees
            : this.data.Trees.filter(t => t.Id === this.activeTreeId);
        const ids = this.collectTreeNodeIds(treesToRender);
        const groups = new Set();
        ids.forEach(id => {
            const node = this.data.Nodes[id];
            if (!node)
                return;
            const group = this.getSubgraphGroup(node, mode);
            groups.add(group);
        });
        return Array.from(groups).sort((a, b) => a.localeCompare(b));
    }
    collectTreeNodeIds(trees) {
        const ids = new Set();
        const traverse = (node) => {
            ids.add(node.Id);
            node.ChildNodes.forEach(child => traverse(child));
        };
        trees.forEach(tree => traverse(tree));
        return ids;
    }
    getSubgraphGroup(node, mode) {
        if (mode === 'project') {
            return this.normalizeGroupName(node.MetaInfo?.ProjectName);
        }
        if (mode === 'folder') {
            return this.normalizeGroupName(this.getFirstLevelFolder(node.MetaInfo));
        }
        return '(None)';
    }
    normalizeGroupName(name) {
        const trimmed = name?.trim();
        return trimmed && trimmed.length > 0 ? trimmed : '(Unknown)';
    }
    getFirstLevelFolder(meta) {
        const path = (meta.DocumentPath || '').replace(/\\/g, '/');
        const segments = path.split('/').filter(Boolean);
        if (segments.length === 0)
            return '(Root)';
        const projectName = meta.ProjectName?.toLowerCase();
        if (projectName) {
            const index = segments.findIndex(seg => seg.toLowerCase() === projectName);
            if (index >= 0 && segments.length > index + 1) {
                return segments[index + 1];
            }
        }
        if (segments.length >= 2) {
            return segments[segments.length - 2];
        }
        return '(Root)';
    }
    getDefaultSubgraphColor(group) {
        const hash = this.hashString(group);
        return this.subgraphPalette[hash % this.subgraphPalette.length];
    }
    getSubgraphColor(mode, group) {
        if (mode === 'none') {
            return this.getDefaultSubgraphColor(group);
        }
        const map = this.subgraphColors[mode] || {};
        if (map[group])
            return map[group];
        const color = this.getDefaultSubgraphColor(group);
        map[group] = color;
        this.subgraphColors[mode] = map;
        return color;
    }
    hashString(value) {
        let hash = 0;
        for (let i = 0; i < value.length; i++) {
            hash = ((hash << 5) - hash) + value.charCodeAt(i);
            hash |= 0;
        }
        return Math.abs(hash);
    }
    buildSubgraphGroups(nodesToRender, mode) {
        const groups = new Map();
        if (!this.data || mode === 'none')
            return groups;
        nodesToRender.forEach(id => {
            const node = this.data.Nodes[id];
            if (!node)
                return;
            const group = this.getSubgraphGroup(node, mode);
            const list = groups.get(group);
            if (list) {
                list.push(id);
            }
            else {
                groups.set(group, [id]);
            }
        });
        return groups;
    }
    buildSubgraphDefinitions(groups, mode) {
        let blocks = '';
        let styles = '';
        let classDefs = '';
        const entries = Array.from(groups.entries()).sort((a, b) => a[0].localeCompare(b[0]));
        entries.forEach(([group, ids], index) => {
            const labelPrefix = mode === 'project' ? 'Project' : 'Folder';
            const label = `${labelPrefix}: ${group}`.replace(/"/g, "'");
            const subgraphId = `${mode}_${this.hashString(group)}_${index}`;
            const className = `sg_${this.hashString(group)}_${index}`;
            blocks += `    subgraph ${subgraphId}["${label}"]\n`;
            blocks += `        direction TB\n`;
            ids.forEach(id => {
                blocks += `        ${id}\n`;
            });
            blocks += `    end\n`;
            const color = this.getSubgraphColor(mode, group);
            styles += `    style ${subgraphId} fill:${color},stroke:#9e9e9e,stroke-width:1px\n`;
            styles += `    class ${subgraphId} ${className}\n`;
            classDefs += `    classDef ${className} fill:${color},stroke:#9e9e9e,stroke-width:1px;\n`;
        });
        return { blocks, styles, classDefs };
    }
    computeVisibleSet(trees) {
        const visibleSet = new Set();
        const traverse = (node, allowedMax, collapseActive) => {
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
    computeEdges(trees, visibleSet) {
        const edges = [];
        const traverse = (node, nearestVisibleAncestor, collapseActive) => {
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
        const nodesToRender = this.computeVisibleSet(treesToRender);
        const edges = this.computeEdges(treesToRender, nodesToRender);
        let graphDef = "graph TD\n";
        const processedNodes = new Set();
        const generateNodeDefinition = (treeNode) => {
            const id = treeNode.Id;
            const data = this.data.Nodes[id];
            const guide = data.Guides;
            const nodeLevel = this.getNodeLevel(id);
            const directHigherChildren = treeNode.ChildNodes.filter(child => this.getNodeLevel(child.Id) > nodeLevel);
            const hasDirectHigherHidden = directHigherChildren.some(child => !nodesToRender.has(child.Id));
            const hasDirectHigherVisible = directHigherChildren.some(child => nodesToRender.has(child.Id));
            const labelText = (guide?.T || data.MetaInfo?.MemberName || "Unknown").replace(/"/g, "'");
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
        };
        const traverseNodes = (treeNode) => {
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
        let subgraphClassDefs = '';
        if (this.subgraphMode !== 'none') {
            const subgraphGroups = this.buildSubgraphGroups(nodesToRender, this.subgraphMode);
            const subgraphDefs = this.buildSubgraphDefinitions(subgraphGroups, this.subgraphMode);
            graphDef += subgraphDefs.blocks;
            graphDef += subgraphDefs.styles;
            subgraphClassDefs = subgraphDefs.classDefs;
        }
        const edgeSet = new Set();
        for (const edge of edges) {
            const key = `${edge.from}-->${edge.to}`;
            if (!edgeSet.has(key)) {
                edgeSet.add(key);
                graphDef += `    ${edge.from} --> ${edge.to}\n`;
            }
        }
        graphDef += `\n    classDef default fill:#e3f2fd,stroke:#333,stroke-width:1px;\n`;
        graphDef += `    classDef highlighted stroke:#ff9800,stroke-width:3px;\n`;
        graphDef += `    classDef tagwarning fill:#fff3e0,stroke:#ffb74d;\n`;
        graphDef += `    classDef tagerror fill:#ffebee,stroke:#ef5350;\n`;
        graphDef += subgraphClassDefs;
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
    renderMetaRow(label, value) {
        if (value === undefined || value === null || value === '')
            return '';
        return `<div class="meta-item"><span class="meta-label">${label}</span><span class="meta-value">${value}</span></div>`;
    }
    hideNodeDetails() {
        const panel = document.getElementById('side-panel');
        if (!panel)
            return;
        panel.classList.remove('open');
        panel.style.display = 'none';
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
            commentsSection.style.display = 'block';
            commentsContent.innerHTML = `<p>${guide.D}</p>`;
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
            const guideText = node.Guides?.T?.toLowerCase() || "";
            const tags = node.Guides?.Tags?.map(t => t.toLowerCase()) || [];
            if (guideText.includes(term) || tags.some(t => t.includes(term))) {
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
        const targetLevel = this.getNodeLevel(id);
        if (targetLevel <= this.baseVisibleLevel)
            return;
        let curr = id;
        while (curr) {
            const existing = this.expandedNodeMaxLevels.get(curr) ?? this.baseVisibleLevel;
            if (targetLevel > existing) {
                this.expandedNodeMaxLevels.set(curr, targetLevel);
            }
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
        const expandedNodeLevels = {};
        this.expandedNodeMaxLevels.forEach((value, key) => {
            expandedNodeLevels[key] = value;
        });
        const config = {
            activeTreeId: this.activeTreeId,
            expandedNodeLevels,
            collapsedNodeIds: Array.from(this.collapsedNodeIds),
            subgraphMode: this.subgraphMode,
            subgraphColors: this.subgraphColors,
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
            this.expandedNodeMaxLevels.clear();
            if (config.expandedNodeLevels) {
                Object.entries(config.expandedNodeLevels).forEach(([id, level]) => {
                    if (typeof level === 'number') {
                        this.expandedNodeMaxLevels.set(id, level);
                    }
                });
            }
            else if (config.expandedNodeIds) {
                config.expandedNodeIds.forEach(id => {
                    const current = this.getCurrentAllowedMax(id);
                    this.expandedNodeMaxLevels.set(id, current + 1);
                });
            }
            this.collapsedNodeIds.clear();
            if (config.collapsedNodeIds) {
                config.collapsedNodeIds.forEach(id => this.collapsedNodeIds.add(id));
            }
            if (config.subgraphMode) {
                this.subgraphMode = config.subgraphMode;
            }
            if (config.subgraphColors) {
                this.subgraphColors = {
                    project: config.subgraphColors.project || {},
                    folder: config.subgraphColors.folder || {}
                };
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