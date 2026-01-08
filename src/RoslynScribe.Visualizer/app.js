"use strict";
const SVG_NS = 'http://www.w3.org/2000/svg';
function sanitizeClassName(value) {
    return value.replace(/[^a-zA-Z0-9_-]/g, '_');
}
function getNodeKeyClass(id) {
    return `node-key-${sanitizeClassName(id)}`;
}
class MermaidRenderer {
    constructor() {
        this.container = null;
        this.panZoomInstance = null;
        this.renderId = 0;
        this.lastGraphDef = null;
        mermaid.initialize({
            startOnLoad: false,
            securityLevel: 'loose',
            htmlLabels: false,
            flowchart: {
                useMaxWidth: false,
                htmlLabels: false,
                curve: 'basis'
            }
        });
    }
    async render(container, model, decorations) {
        this.container = container;
        if (this.panZoomInstance && this.panZoomInstance.destroy) {
            this.panZoomInstance.destroy();
            this.panZoomInstance = null;
        }
        const graphDef = this.buildGraphDefinition(model);
        this.lastGraphDef = graphDef;
        if (typeof window !== 'undefined') {
            window.lastMermaidGraphDefRaw = graphDef;
        }
        let isValid = false;
        try {
            isValid = await mermaid.parse(graphDef);
        }
        catch (error) {
            this.logGraphDefinition(graphDef);
            throw error;
        }
        if (!isValid) {
            this.logGraphDefinition(graphDef);
            throw new Error("Graph parsing failed");
        }
        const { svg } = await mermaid.render(`graphDiv_${this.renderId++}`, graphDef);
        container.innerHTML = svg;
        const svgEl = container.querySelector('svg');
        if (svgEl) {
            svgEl.setAttribute('width', '100%');
            svgEl.setAttribute('height', '100%');
            svgEl.style.width = '100%';
            svgEl.style.height = '100%';
        }
        this.hydrateNodeDecorations(container, decorations);
        if (svgEl) {
            this.panZoomInstance = svgPanZoom(svgEl, {
                zoomEnabled: true,
                controlIconsEnabled: true,
                fit: true,
                center: true
            });
        }
    }
    focusNode(nodeId) {
        if (!this.container || !this.panZoomInstance)
            return;
        const nodeClass = getNodeKeyClass(nodeId);
        const el = this.container.querySelector(`svg g.node.${nodeClass}`);
        if (!el)
            return;
        const bbox = el.getBBox();
        const sizes = this.panZoomInstance.getSizes();
        const zoom = this.panZoomInstance.getZoom();
        const cx = bbox.x + bbox.width / 2;
        const cy = bbox.y + bbox.height / 2;
        const newPanX = (sizes.width / 2) - (cx * zoom);
        const newPanY = (sizes.height / 2) - (cy * zoom);
        this.panZoomInstance.pan({ x: newPanX, y: newPanY });
        this.panZoomInstance.setZoom(1.2);
    }
    getLastGraphDefinition() {
        return this.lastGraphDef;
    }
    sanitizeMermaidLabel(value) {
        return value.replace(/[\r\n]+/g, ' ').replace(/"/g, '\\"');
    }
    serializeStyles(styles) {
        return Object.entries(styles)
            .map(([key, value]) => `${key}:${value}`)
            .join(',');
    }
    buildGraphDefinition(model) {
        let graphDef = `flowchart ${this.getMermaidDirection(model.direction)}\n`;
        const subgraphStyleLines = [];
        const edgesBySubgraph = new Map();
        const rootEdges = [];
        const nodeMembership = new Map();
        const getSubgraphLevelFromId = (id) => {
            if (id.startsWith('solution_'))
                return 'solution';
            if (id.startsWith('project_'))
                return 'project';
            if (id.startsWith('folder_'))
                return 'folder';
            if (id.startsWith('type_'))
                return 'type';
            return null;
        };
        const recordMembership = (subgraph, path) => {
            const level = getSubgraphLevelFromId(subgraph.id);
            const nextPath = { ...path };
            if (level === 'solution')
                nextPath.solutionId = subgraph.id;
            if (level === 'project')
                nextPath.projectId = subgraph.id;
            if (level === 'folder')
                nextPath.folderId = subgraph.id;
            if (level === 'type')
                nextPath.typeId = subgraph.id;
            subgraph.nodeIds.forEach(nodeId => {
                nodeMembership.set(nodeId, nextPath);
            });
            subgraph.subgraphs?.forEach(child => recordMembership(child, nextPath));
        };
        model.subgraphs.forEach(subgraph => recordMembership(subgraph, {}));
        const appendEdges = (edges, indent) => {
            edges.forEach(edge => {
                graphDef += `${indent}${edge.from} --> ${edge.to}\n`;
            });
        };
        model.nodes.forEach(node => {
            const cleanLabel = this.sanitizeMermaidLabel(node.label);
            graphDef += `    ${node.id}["${cleanLabel}"]\n`;
            if (node.classes.length > 0) {
                graphDef += `    class ${node.id} ${node.classes.join(',')}\n`;
            }
        });
        model.edges.forEach(edge => {
            const fromPath = nodeMembership.get(edge.from);
            const toPath = nodeMembership.get(edge.to);
            let targetSubgraphId = null;
            if (fromPath && toPath) {
                if (fromPath.typeId && fromPath.typeId === toPath.typeId) {
                    targetSubgraphId = fromPath.typeId;
                }
                else if (fromPath.folderId && fromPath.folderId === toPath.folderId) {
                    targetSubgraphId = fromPath.folderId;
                }
                else if (fromPath.projectId && fromPath.projectId === toPath.projectId) {
                    targetSubgraphId = fromPath.projectId;
                }
                else if (fromPath.solutionId && fromPath.solutionId === toPath.solutionId) {
                    targetSubgraphId = fromPath.solutionId;
                }
            }
            if (targetSubgraphId) {
                const list = edgesBySubgraph.get(targetSubgraphId);
                if (list) {
                    list.push(edge);
                }
                else {
                    edgesBySubgraph.set(targetSubgraphId, [edge]);
                }
            }
            else {
                rootEdges.push(edge);
            }
        });
        const appendSubgraph = (subgraph, indent) => {
            const cleanLabel = this.sanitizeMermaidLabel(subgraph.label);
            graphDef += `${indent}subgraph ${subgraph.id}["${cleanLabel}"]\n`;
            graphDef += `${indent}    direction ${this.getMermaidDirection(subgraph.direction)}\n`;
            subgraph.subgraphs?.forEach(child => {
                appendSubgraph(child, `${indent}    `);
            });
            subgraph.nodeIds.forEach(id => {
                graphDef += `${indent}    ${id}\n`;
            });
            const localEdges = edgesBySubgraph.get(subgraph.id);
            if (localEdges && localEdges.length > 0) {
                appendEdges(localEdges, `${indent}    `);
            }
            graphDef += `${indent}end\n`;
            if (subgraph.classNames.length > 0) {
                subgraphStyleLines.push(`    class ${subgraph.id} ${subgraph.classNames.join(',')}`);
            }
            subgraphStyleLines.push(`    style ${subgraph.id} ${this.serializeStyles(subgraph.styles)}`);
        };
        model.subgraphs.forEach(subgraph => {
            appendSubgraph(subgraph, '    ');
        });
        appendEdges(rootEdges, '    ');
        subgraphStyleLines.forEach(line => {
            graphDef += `${line}\n`;
        });
        graphDef += '\n';
        model.classDefs.forEach(classDef => {
            graphDef += `    classDef ${classDef.name} ${this.serializeStyles(classDef.styles)};\n`;
        });
        return graphDef;
    }
    logGraphDefinition(graphDef) {
        const lines = graphDef.split(/\r?\n/);
        const numbered = lines
            .map((line, index) => `${String(index + 1).padStart(4, ' ')}| ${line}`)
            .join('\n');
        console.error('Mermaid definition:\n' + numbered);
        if (typeof window !== 'undefined') {
            window.lastMermaidGraphDef = numbered;
        }
    }
    getMermaidDirection(direction) {
        return direction;
    }
    createSvgElement(tag) {
        return document.createElementNS(SVG_NS, tag);
    }
    buildIcon(icon) {
        const iconGroup = this.createSvgElement('g');
        iconGroup.setAttribute('class', 'node-icon');
        iconGroup.setAttribute('fill', 'none');
        iconGroup.setAttribute('stroke', 'currentColor');
        iconGroup.setAttribute('stroke-width', '2');
        iconGroup.setAttribute('stroke-linecap', 'round');
        iconGroup.setAttribute('stroke-linejoin', 'round');
        if (icon === 'expand') {
            const vLine = this.createSvgElement('line');
            vLine.setAttribute('x1', '12');
            vLine.setAttribute('y1', '5');
            vLine.setAttribute('x2', '12');
            vLine.setAttribute('y2', '19');
            iconGroup.appendChild(vLine);
            const hLine = this.createSvgElement('line');
            hLine.setAttribute('x1', '5');
            hLine.setAttribute('y1', '12');
            hLine.setAttribute('x2', '19');
            hLine.setAttribute('y2', '12');
            iconGroup.appendChild(hLine);
        }
        else if (icon === 'collapse') {
            const hLine = this.createSvgElement('line');
            hLine.setAttribute('x1', '5');
            hLine.setAttribute('y1', '12');
            hLine.setAttribute('x2', '19');
            hLine.setAttribute('y2', '12');
            iconGroup.appendChild(hLine);
        }
        else {
            const circle = this.createSvgElement('circle');
            circle.setAttribute('cx', '12');
            circle.setAttribute('cy', '12');
            circle.setAttribute('r', '10');
            iconGroup.appendChild(circle);
            const vLine = this.createSvgElement('line');
            vLine.setAttribute('x1', '12');
            vLine.setAttribute('y1', '16');
            vLine.setAttribute('x2', '12');
            vLine.setAttribute('y2', '12');
            iconGroup.appendChild(vLine);
            const dot = this.createSvgElement('line');
            dot.setAttribute('x1', '12');
            dot.setAttribute('y1', '8');
            dot.setAttribute('x2', '12.01');
            dot.setAttribute('y2', '8');
            iconGroup.appendChild(dot);
        }
        return iconGroup;
    }
    appendIconButton(parent, x, y, size, icon, action, nodeId, title) {
        const buttonGroup = this.createSvgElement('g');
        buttonGroup.setAttribute('class', 'node-icon-btn');
        buttonGroup.setAttribute('data-action', action);
        buttonGroup.setAttribute('data-id', nodeId);
        buttonGroup.setAttribute('transform', `translate(${x}, ${y})`);
        buttonGroup.setAttribute('role', 'button');
        buttonGroup.setAttribute('aria-label', title);
        const bg = this.createSvgElement('rect');
        bg.setAttribute('class', 'node-icon-bg');
        bg.setAttribute('x', '0');
        bg.setAttribute('y', '0');
        bg.setAttribute('width', `${size}`);
        bg.setAttribute('height', `${size}`);
        bg.setAttribute('rx', '4');
        bg.setAttribute('ry', '4');
        buttonGroup.appendChild(bg);
        const iconGroup = this.buildIcon(icon);
        const iconSize = size - 8;
        const scale = iconSize / 24;
        const offset = (size - iconSize) / 2;
        iconGroup.setAttribute('transform', `translate(${offset}, ${offset}) scale(${scale})`);
        buttonGroup.appendChild(iconGroup);
        const titleEl = this.createSvgElement('title');
        titleEl.textContent = title;
        buttonGroup.appendChild(titleEl);
        parent.appendChild(buttonGroup);
    }
    appendBadge(parent, x, y, count) {
        const badgeGroup = this.createSvgElement('g');
        badgeGroup.setAttribute('class', 'node-badge');
        badgeGroup.setAttribute('transform', `translate(${x}, ${y})`);
        const circle = this.createSvgElement('circle');
        circle.setAttribute('class', 'node-badge-circle');
        circle.setAttribute('cx', '0');
        circle.setAttribute('cy', '0');
        circle.setAttribute('r', '9');
        circle.setAttribute('fill', '#1565c0');
        circle.setAttribute('stroke', '#fff');
        circle.setAttribute('stroke-width', '1');
        circle.setAttribute('style', 'fill: #1565c0 !important; stroke: #fff !important; stroke-width: 1px !important;');
        badgeGroup.appendChild(circle);
        const text = this.createSvgElement('text');
        text.setAttribute('class', 'node-badge-text');
        text.setAttribute('x', '0');
        text.setAttribute('y', '0');
        text.setAttribute('text-anchor', 'middle');
        text.setAttribute('dominant-baseline', 'middle');
        text.textContent = `+${count}`;
        badgeGroup.appendChild(text);
        parent.appendChild(badgeGroup);
    }
    hydrateNodeDecorations(container, nodeDecorations) {
        const svg = container.querySelector('svg');
        if (!svg)
            return;
        nodeDecorations.forEach(decoration => {
            const nodeEl = svg.querySelector(`g.node.${decoration.className}`);
            if (!nodeEl)
                return;
            const bbox = nodeEl.getBBox();
            const rect = nodeEl.querySelector('rect.label-container');
            const rectBox = rect ? rect.getBBox() : bbox;
            const decorationGroup = this.createSvgElement('g');
            decorationGroup.setAttribute('class', 'node-decorations');
            decorationGroup.setAttribute('data-node-id', decoration.id);
            const badgePadding = 2;
            if (decoration.hiddenCount > 0) {
                const badgeX = rectBox.x + rectBox.width - 9 - badgePadding;
                const badgeY = rectBox.y + 9 + badgePadding;
                this.appendBadge(decorationGroup, badgeX, badgeY, decoration.hiddenCount);
            }
            const buttons = [];
            if (decoration.canExpand) {
                buttons.push({ action: 'expand', icon: 'expand', title: 'Expand' });
            }
            if (decoration.canCollapse) {
                buttons.push({ action: 'collapse', icon: 'collapse', title: 'Retract' });
            }
            buttons.push({ action: 'details', icon: 'details', title: 'Details' });
            const buttonSize = 20;
            const buttonGap = 6;
            const totalWidth = (buttonSize * buttons.length) + (buttonGap * (buttons.length - 1));
            const startX = rectBox.x + (rectBox.width - totalWidth) / 2;
            const y = rectBox.y + rectBox.height + 6;
            buttons.forEach((button, index) => {
                const x = startX + (index * (buttonSize + buttonGap));
                this.appendIconButton(decorationGroup, x, y, buttonSize, button.icon, button.action, decoration.id, button.title);
            });
            nodeEl.appendChild(decorationGroup);
        });
    }
}
class ScribeApp {
    constructor() {
        this.data = null;
        this.childToParentMap = new Map();
        this.expandedNodeMaxLevels = new Map();
        this.collapsedNodeIds = new Set();
        this.activeTreeId = null;
        this.baseVisibleLevel = 1;
        this.graphDirection = 'LR';
        this.subgraphSettings = {
            solution: { visible: true, direction: 'LR', colors: {} },
            project: { visible: true, direction: 'LR', colors: {} },
            folder: { visible: true, direction: 'LR', colors: {} },
            type: { visible: true, direction: 'LR', colors: {} }
        };
        this.subgraphPalette = ['#e8f5e9', '#e3f2fd', '#fff3e0', '#f3e5f5', '#e0f7fa', '#fce4ec'];
        this.folderSubgraphColor = '#fff3e0';
        this.typeSubgraphColor = '#e8f5e9';
        this.searchResults = [];
        this.currentSearchIndex = -1;
        this.subgraphIdsByLevel = {
            solution: [],
            project: [],
            folder: [],
            type: []
        };
        this.renderer = new MermaidRenderer();
        this.initializeEventListeners();
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
        document.getElementById('btn-download-mermaid')?.addEventListener('click', () => {
            this.downloadMermaidDefinition();
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
        document.getElementById('btn-close-details')?.addEventListener('click', () => {
            this.hideNodeDetails();
        });
        const mermaidOutput = document.getElementById('mermaid-output');
        mermaidOutput?.addEventListener('click', (e) => {
            let target = e.target;
            while (target && target !== mermaidOutput) {
                const action = target.getAttribute('data-action');
                const id = target.getAttribute('data-id');
                if (action && id) {
                    if (action === 'expand') {
                        this.expandNode(id);
                        e.stopPropagation();
                        return;
                    }
                    if (action === 'details') {
                        this.showNodeDetails(id);
                        e.stopPropagation();
                        return;
                    }
                    if (action === 'collapse') {
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
            this.warnAboutMissingNodeData();
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
    warnAboutMissingNodeData() {
        if (!this.data)
            return;
        const allTreeIds = this.collectTreeNodeIds(this.data.Trees);
        const missingIds = Array.from(allTreeIds).filter(id => !this.data.Nodes[id]);
        if (missingIds.length === 0)
            return;
        const preview = missingIds.slice(0, 20).join(', ');
        console.warn('Loaded .adc.json references node IDs not present in Nodes:', missingIds);
        alert(`Warning: Loaded file references ${missingIds.length} node(s) that are missing from the Nodes dictionary.\n\n` +
            `The graph will render with placeholders.\n\n` +
            `Missing IDs (first ${Math.min(20, missingIds.length)}): ${preview}`);
    }
    enableControls() {
        const ids = ['tree-select', 'search-input', 'btn-reset', 'btn-save-config', 'btn-load-config', 'btn-edit-config', 'btn-download-mermaid'];
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
        const graphDirectionSelect = document.getElementById('graph-direction-select');
        if (!modal || !graphDirectionSelect)
            return;
        graphDirectionSelect.value = this.graphDirection;
        this.applySubgraphControlsToModal();
        this.renderSubgraphColorLists();
        modal.classList.add('open');
    }
    closeConfigModal() {
        const modal = document.getElementById('config-modal');
        if (!modal)
            return;
        modal.classList.remove('open');
    }
    applyConfigModal() {
        const graphDirectionSelect = document.getElementById('graph-direction-select');
        if (!graphDirectionSelect)
            return;
        const nextGraphDirection = graphDirectionSelect.value;
        const nextSolution = this.readSubgraphControlsFromModal('solution');
        const nextProject = this.readSubgraphControlsFromModal('project');
        const nextFolder = this.readSubgraphControlsFromModal('folder');
        const nextType = this.readSubgraphControlsFromModal('type');
        if (!nextSolution || !nextProject || !nextFolder || !nextType)
            return;
        const visibilityChanged = (nextSolution.visible !== this.subgraphSettings.solution.visible ||
            nextProject.visible !== this.subgraphSettings.project.visible ||
            nextFolder.visible !== this.subgraphSettings.folder.visible ||
            nextType.visible !== this.subgraphSettings.type.visible);
        const directionChanged = (nextGraphDirection !== this.graphDirection ||
            nextSolution.direction !== this.subgraphSettings.solution.direction ||
            nextProject.direction !== this.subgraphSettings.project.direction ||
            nextFolder.direction !== this.subgraphSettings.folder.direction ||
            nextType.direction !== this.subgraphSettings.type.direction);
        this.graphDirection = nextGraphDirection;
        this.subgraphSettings.solution.visible = nextSolution.visible;
        this.subgraphSettings.solution.direction = nextSolution.direction;
        this.subgraphSettings.project.visible = nextProject.visible;
        this.subgraphSettings.project.direction = nextProject.direction;
        this.subgraphSettings.folder.visible = nextFolder.visible;
        this.subgraphSettings.folder.direction = nextFolder.direction;
        this.subgraphSettings.type.visible = nextType.visible;
        this.subgraphSettings.type.direction = nextType.direction;
        const colorsChanged = (this.applySubgraphColorsFromModal('solution') ||
            this.applySubgraphColorsFromModal('project') ||
            this.applySubgraphColorsFromModal('folder') ||
            this.applySubgraphColorsFromModal('type'));
        this.closeConfigModal();
        if (directionChanged || colorsChanged) {
            this.renderGraph();
            return;
        }
        if (visibilityChanged) {
            this.applySubgraphVisibility();
        }
    }
    applySubgraphControlsToModal() {
        this.setSubgraphControlValues('solution');
        this.setSubgraphControlValues('project');
        this.setSubgraphControlValues('folder');
        this.setSubgraphControlValues('type');
    }
    setSubgraphControlValues(level) {
        const visibleInput = document.getElementById(`subgraph-${level}-visible`);
        const directionSelect = document.getElementById(`subgraph-${level}-direction`);
        if (visibleInput)
            visibleInput.checked = this.subgraphSettings[level].visible;
        if (directionSelect)
            directionSelect.value = this.subgraphSettings[level].direction;
    }
    readSubgraphControlsFromModal(level) {
        const visibleInput = document.getElementById(`subgraph-${level}-visible`);
        const directionSelect = document.getElementById(`subgraph-${level}-direction`);
        if (!visibleInput || !directionSelect)
            return null;
        return {
            visible: visibleInput.checked,
            direction: directionSelect.value
        };
    }
    applySubgraphColorsFromModal(level) {
        if (level === 'folder' || level === 'type')
            return false;
        const list = document.getElementById(`subgraph-colors-${level}`);
        if (!list)
            return false;
        const inputs = Array.from(list.querySelectorAll('input[type="color"]'));
        const map = { ...(this.subgraphSettings[level].colors || {}) };
        let changed = false;
        inputs.forEach(input => {
            const group = input.dataset.group;
            if (group) {
                if (map[group] !== input.value) {
                    map[group] = input.value;
                    changed = true;
                }
            }
        });
        if (changed) {
            this.subgraphSettings[level].colors = map;
        }
        return changed;
    }
    renderSubgraphColorLists() {
        this.renderSubgraphColorList('solution');
        this.renderSubgraphColorList('project');
        this.renderSubgraphColorList('folder');
        this.renderSubgraphColorList('type');
    }
    renderSubgraphColorList(level) {
        const list = document.getElementById(`subgraph-colors-${level}`);
        if (!list)
            return;
        list.innerHTML = '';
        if (level === 'folder' || level === 'type') {
            const row = document.createElement('div');
            row.className = 'color-row';
            const label = document.createElement('label');
            const input = document.createElement('input');
            const inputId = `subgraph-color-${level}-fixed`;
            label.setAttribute('for', inputId);
            label.textContent = level === 'folder' ? 'All folders' : 'All types';
            input.type = 'color';
            input.id = inputId;
            input.value = level === 'folder' ? this.folderSubgraphColor : this.typeSubgraphColor;
            input.disabled = true;
            row.appendChild(label);
            row.appendChild(input);
            list.appendChild(row);
            return;
        }
        const groups = this.getSubgraphGroups(level);
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
            const inputId = `subgraph-color-${level}-${index}`;
            label.setAttribute('for', inputId);
            label.textContent = group;
            input.type = 'color';
            input.id = inputId;
            input.dataset.group = group;
            input.value = this.subgraphSettings[level].colors?.[group] ?? this.getDefaultSubgraphColor(group);
            row.appendChild(label);
            row.appendChild(input);
            list.appendChild(row);
        });
    }
    getSubgraphGroups(level) {
        if (!this.data)
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
            const group = this.getSubgraphGroup(node, level);
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
    getSubgraphGroup(node, level) {
        if (level === 'solution') {
            return this.normalizeGroupName(node.MetaInfo?.SolutionName);
        }
        if (level === 'project') {
            return this.normalizeGroupName(node.MetaInfo?.ProjectName);
        }
        if (level === 'type') {
            return this.normalizeGroupName(node.MetaInfo?.TypeName);
        }
        return this.normalizeGroupName(this.getFirstLevelFolder(node.MetaInfo));
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
    getSubgraphColor(level, group) {
        if (level === 'folder')
            return this.folderSubgraphColor;
        if (level === 'type')
            return this.typeSubgraphColor;
        const map = this.subgraphSettings[level].colors || {};
        if (map[group])
            return map[group];
        const color = this.getDefaultSubgraphColor(group);
        map[group] = color;
        this.subgraphSettings[level].colors = map;
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
    getSubgraphLevelClass(level) {
        return `sg-level-${level}`;
    }
    getSubgraphLabelPrefix(level) {
        if (level === 'solution')
            return 'Solution';
        if (level === 'project')
            return 'Project';
        if (level === 'folder')
            return 'Folder';
        return 'Type';
    }
    buildSubgraphHierarchy(nodesToRender) {
        const hierarchy = new Map();
        if (!this.data)
            return hierarchy;
        nodesToRender.forEach(id => {
            const node = this.data.Nodes[id];
            if (!node)
                return;
            const solution = this.getSubgraphGroup(node, 'solution');
            const project = this.getSubgraphGroup(node, 'project');
            const folder = this.getSubgraphGroup(node, 'folder');
            const type = this.getSubgraphGroup(node, 'type');
            let projectMap = hierarchy.get(solution);
            if (!projectMap) {
                projectMap = new Map();
                hierarchy.set(solution, projectMap);
            }
            let folderMap = projectMap.get(project);
            if (!folderMap) {
                folderMap = new Map();
                projectMap.set(project, folderMap);
            }
            let typeMap = folderMap.get(folder);
            if (!typeMap) {
                typeMap = new Map();
                folderMap.set(folder, typeMap);
            }
            const list = typeMap.get(type);
            if (list) {
                list.push(id);
            }
            else {
                typeMap.set(type, [id]);
            }
        });
        return hierarchy;
    }
    buildSubgraphModels(hierarchy) {
        const subgraphs = [];
        const classDefs = [];
        const definedClasses = new Set();
        const createSubgraph = (level, group, key, nodeIds, children) => {
            const label = `${this.getSubgraphLabelPrefix(level)}: ${group}`;
            const className = `sg_${level}_${this.hashString(key)}`;
            const color = this.getSubgraphColor(level, group);
            const styles = {
                fill: color,
                stroke: '#9e9e9e',
                'stroke-width': '1px'
            };
            if (!definedClasses.has(className)) {
                definedClasses.add(className);
                classDefs.push({ name: className, styles });
            }
            return {
                id: `${level}_${this.hashString(key)}`,
                label,
                nodeIds,
                classNames: [className, this.getSubgraphLevelClass(level)],
                styles,
                direction: this.subgraphSettings[level].direction,
                subgraphs: children
            };
        };
        const solutionEntries = Array.from(hierarchy.entries()).sort((a, b) => a[0].localeCompare(b[0]));
        solutionEntries.forEach(([solutionName, projectMap]) => {
            const projectEntries = Array.from(projectMap.entries()).sort((a, b) => a[0].localeCompare(b[0]));
            const projectSubgraphs = [];
            projectEntries.forEach(([projectName, folderMap]) => {
                const folderEntries = Array.from(folderMap.entries()).sort((a, b) => a[0].localeCompare(b[0]));
                const folderSubgraphs = [];
                folderEntries.forEach(([folderName, typeMap]) => {
                    const typeEntries = Array.from(typeMap.entries()).sort((a, b) => a[0].localeCompare(b[0]));
                    const typeSubgraphs = typeEntries.map(([typeName, ids]) => {
                        const typeKey = `solution:${solutionName}|project:${projectName}|folder:${folderName}|type:${typeName}`;
                        return createSubgraph('type', typeName, typeKey, ids, []);
                    });
                    const folderKey = `solution:${solutionName}|project:${projectName}|folder:${folderName}`;
                    folderSubgraphs.push(createSubgraph('folder', folderName, folderKey, [], typeSubgraphs));
                });
                const projectKey = `solution:${solutionName}|project:${projectName}`;
                projectSubgraphs.push(createSubgraph('project', projectName, projectKey, [], folderSubgraphs));
            });
            const solutionKey = `solution:${solutionName}`;
            subgraphs.push(createSubgraph('solution', solutionName, solutionKey, [], projectSubgraphs));
        });
        return { subgraphs, classDefs };
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
        const model = {
            direction: this.graphDirection,
            nodes: [],
            edges: [],
            subgraphs: [],
            classDefs: []
        };
        const processedNodes = new Set();
        const nodeDecorations = new Map();
        const generateNodeDefinition = (treeNode) => {
            const id = treeNode.Id;
            const data = this.data.Nodes[id];
            const guide = data.Guides;
            const nodeLevel = this.getNodeLevel(id);
            const directHigherChildren = treeNode.ChildNodes.filter(child => this.getNodeLevel(child.Id) > nodeLevel);
            const hasDirectHigherHidden = directHigherChildren.some(child => !nodesToRender.has(child.Id));
            const hasDirectHigherVisible = directHigherChildren.some(child => nodesToRender.has(child.Id));
            const labelText = guide?.T || data.MetaInfo?.MemberName || "Unknown";
            let hiddenCount = 0;
            if (treeNode.ChildNodes) {
                for (const child of treeNode.ChildNodes) {
                    if (!nodesToRender.has(child.Id)) {
                        hiddenCount++;
                    }
                }
            }
            const canExpand = hasDirectHigherHidden;
            const canRetract = hasDirectHigherVisible;
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
            const nodeKeyClass = getNodeKeyClass(id);
            nodeDecorations.set(id, {
                id,
                className: nodeKeyClass,
                hiddenCount,
                canExpand,
                canCollapse: canRetract
            });
            const nodeClasses = [nodeKeyClass, ...styles];
            model.nodes.push({
                id,
                label: labelText,
                classes: nodeClasses
            });
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
        const subgraphHierarchy = this.buildSubgraphHierarchy(nodesToRender);
        const subgraphModels = this.buildSubgraphModels(subgraphHierarchy);
        model.subgraphs = subgraphModels.subgraphs;
        model.classDefs.push(...subgraphModels.classDefs);
        this.cacheSubgraphIdsByLevel(model.subgraphs);
        const edgeSet = new Set();
        for (const edge of edges) {
            const key = `${edge.from}-->${edge.to}`;
            if (!edgeSet.has(key)) {
                edgeSet.add(key);
                model.edges.push(edge);
            }
        }
        model.classDefs.push({
            name: 'default',
            styles: { fill: '#e3f2fd', stroke: '#333', 'stroke-width': '1px' }
        }, {
            name: 'highlighted',
            styles: { stroke: '#ff9800', 'stroke-width': '3px' }
        }, {
            name: 'tagwarning',
            styles: { fill: '#fff3e0', stroke: '#ffb74d' }
        }, {
            name: 'tagerror',
            styles: { fill: '#ffebee', stroke: '#ef5350' }
        });
        try {
            await this.renderer.render(container, model, nodeDecorations);
            this.applySubgraphVisibility();
        }
        catch (e) {
            console.error("Graph Render Error", e);
            container.innerText = "Error rendering graph.";
        }
        finally {
            this.showLoading(false);
        }
    }
    downloadMermaidDefinition() {
        const definition = this.renderer.getLastGraphDefinition();
        if (!definition) {
            alert('No Mermaid definition available yet.');
            return;
        }
        const blob = new Blob([definition], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'roslyn-scribe-graph.mmd';
        a.click();
        URL.revokeObjectURL(url);
    }
    applySubgraphVisibility() {
        const container = document.getElementById('mermaid-output');
        const svg = container?.querySelector('svg');
        if (!svg)
            return;
        const levels = ['solution', 'project', 'folder', 'type'];
        levels.forEach(level => {
            const subgraphIds = this.subgraphIdsByLevel[level];
            subgraphIds.forEach(id => {
                const selectors = [
                    `g.cluster#${id}`,
                    `g.cluster#cluster_${id}`,
                    `g.cluster#cluster-${id}`,
                    `#${id} g.cluster`,
                    `#cluster_${id}`,
                    `#cluster-${id}`
                ];
                const cluster = selectors
                    .map(selector => svg.querySelector(selector))
                    .find((element) => !!element);
                if (!cluster)
                    return;
                if (this.subgraphSettings[level].visible) {
                    cluster.classList.remove('sg-hidden');
                }
                else {
                    cluster.classList.add('sg-hidden');
                }
            });
        });
    }
    cacheSubgraphIdsByLevel(subgraphs) {
        const idsByLevel = {
            solution: [],
            project: [],
            folder: [],
            type: []
        };
        const visit = (subgraph) => {
            const level = this.getSubgraphLevelFromId(subgraph.id);
            if (level) {
                idsByLevel[level].push(subgraph.id);
            }
            subgraph.subgraphs?.forEach(child => visit(child));
        };
        subgraphs.forEach(subgraph => visit(subgraph));
        this.subgraphIdsByLevel = idsByLevel;
    }
    getSubgraphLevelFromId(id) {
        if (id.startsWith('solution_'))
            return 'solution';
        if (id.startsWith('project_'))
            return 'project';
        if (id.startsWith('folder_'))
            return 'folder';
        if (id.startsWith('type_'))
            return 'type';
        return null;
    }
    renderMetaRow(label, value) {
        const isEmpty = value === undefined || value === null || value === '';
        const displayValue = isEmpty ? '(empty)' : value;
        return `<div class="meta-item"><span class="meta-label">${label}</span><span class="meta-value">${displayValue}</span></div>`;
    }
    renderMetaSection(title, rows) {
        return `<div class="meta-section"><h4 class="meta-section-title">${title}</h4><hr class="meta-section-divider">${rows.join('')}</div>`;
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
        const originIds = guide?.OUI?.length ? guide.OUI.join(', ') : '';
        const destinationUserIds = guide?.DUI?.length ? guide.DUI.join(', ') : '';
        const childNodeIds = data.ChildNodeIds?.length ? data.ChildNodeIds.join(', ') : '';
        const sections = [
            this.renderMetaSection('ScribeNodeData', [
                this.renderMetaRow('Id', data.Id),
                this.renderMetaRow('User Id', guide?.Uid),
                this.renderMetaRow('Kind', data.Kind),
                this.renderMetaRow('Level', guide?.L),
                this.renderMetaRow('ChildNodeIds', childNodeIds),
            ]),
            this.renderMetaSection('Guides', [
                this.renderMetaRow('Text', guide?.T),
                this.renderMetaRow('Description', guide?.D),
                this.renderMetaRow('Path', guide?.P),
                this.renderMetaRow('OriginIds', originIds),
                this.renderMetaRow('DestinationUserIds', destinationUserIds),
                this.renderMetaRow('Tags', tags),
            ]),
            this.renderMetaSection('MetaInfo', [
                this.renderMetaRow('ProjectName', meta.ProjectName),
                this.renderMetaRow('DocumentName', meta.DocumentName),
                this.renderMetaRow('DocumentPath', meta.DocumentPath),
                this.renderMetaRow('NameSpace', meta.NameSpace),
                this.renderMetaRow('TypeName', meta.TypeName),
                this.renderMetaRow('Member Name', meta.MemberName),
                this.renderMetaRow('Identifier (Meta)', meta.Identifier),
                this.renderMetaRow('Line', meta.Line),
            ])
        ];
        content.innerHTML = sections.join('');
        if (commentsSection) {
            commentsSection.style.display = 'none';
        }
        if (commentsContent) {
            commentsContent.innerHTML = '';
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
        this.renderer.focusNode(id);
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
            graphDirection: this.graphDirection,
            subgraphs: {
                solution: {
                    visible: this.subgraphSettings.solution.visible,
                    direction: this.subgraphSettings.solution.direction,
                    colors: this.subgraphSettings.solution.colors
                },
                project: {
                    visible: this.subgraphSettings.project.visible,
                    direction: this.subgraphSettings.project.direction,
                    colors: this.subgraphSettings.project.colors
                },
                folder: {
                    visible: this.subgraphSettings.folder.visible,
                    direction: this.subgraphSettings.folder.direction,
                    colors: this.subgraphSettings.folder.colors
                },
                type: {
                    visible: this.subgraphSettings.type.visible,
                    direction: this.subgraphSettings.type.direction,
                    colors: this.subgraphSettings.type.colors
                }
            },
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
            if (config.graphDirection) {
                this.graphDirection = config.graphDirection;
            }
            if (config.subgraphs) {
                const applySubgraphConfig = (level, subgraph) => {
                    if (!subgraph)
                        return;
                    if (typeof subgraph.visible === 'boolean') {
                        this.subgraphSettings[level].visible = subgraph.visible;
                    }
                    if (subgraph.direction) {
                        this.subgraphSettings[level].direction = subgraph.direction;
                    }
                    if (subgraph.colors) {
                        this.subgraphSettings[level].colors = subgraph.colors;
                    }
                };
                applySubgraphConfig('solution', config.subgraphs.solution);
                applySubgraphConfig('project', config.subgraphs.project);
                applySubgraphConfig('folder', config.subgraphs.folder);
                applySubgraphConfig('type', config.subgraphs.type);
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