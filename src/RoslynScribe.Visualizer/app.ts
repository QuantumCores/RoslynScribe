/// <reference path="types.ts" />
/// <reference path="renderers.ts" />

type SubgraphLevel = 'solution' | 'project' | 'folder' | 'type';

type SubgraphConfig = {
    visible: boolean;
    direction: GraphDirection;
    colors: Record<string, string>;
};

type SubgraphSettings = Record<SubgraphLevel, SubgraphConfig>;

class ScribeApp {
    private data: ScribeResult | null = null;
    private childToParentMap: Map<string, string> = new Map();
    private expandedNodeMaxLevels: Map<string, number> = new Map();
    private collapsedNodeIds: Set<string> = new Set();
    private activeTreeId: string | null = null;
    private renderer: GraphRenderer;
    private readonly baseVisibleLevel = 1;
    private graphDirection: GraphDirection = 'LR';
    private subgraphSettings: SubgraphSettings = {
        solution: { visible: true, direction: 'LR', colors: {} },
        project: { visible: true, direction: 'LR', colors: {} },
        folder: { visible: true, direction: 'LR', colors: {} },
        type: { visible: true, direction: 'LR', colors: {} }
    };
    private readonly subgraphPalette = ['#e8f5e9', '#e3f2fd', '#fff3e0', '#f3e5f5', '#e0f7fa', '#fce4ec'];
    private readonly folderSubgraphColor = '#fff3e0';
    private readonly typeSubgraphColor = '#e8f5e9';
    
    // Search State
    private searchResults: string[] = [];
    private currentSearchIndex: number = -1;

    constructor() {
        this.renderer = new MermaidRenderer();
        this.initializeEventListeners();

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
        document.getElementById('btn-download-mermaid')?.addEventListener('click', () => {
            this.downloadMermaidDefinition();
        });
        document.getElementById('config-input')?.addEventListener('change', (e: Event) => {
            const file = (e.target as HTMLInputElement).files?.[0];
            if (file) this.loadConfigFile(file);
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

        // Global Event Delegation for Dynamic Mermaid Elements
        // This is robust against parsing errors and scope issues
        const mermaidOutput = document.getElementById('mermaid-output');
        mermaidOutput?.addEventListener('click', (e: Event) => {
            let target = e.target as Element | null;
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
            this.warnAboutMissingNodeData();
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

    private warnAboutMissingNodeData() {
        if (!this.data) return;

        const allTreeIds = this.collectTreeNodeIds(this.data.Trees);
        const missingIds = Array.from(allTreeIds).filter(id => !this.data!.Nodes[id]);
        if (missingIds.length === 0) return;

        // Keep this non-fatal: render will show placeholders for missing nodes.
        const preview = missingIds.slice(0, 20).join(', ');
        console.warn('Loaded .adc.json references node IDs not present in Nodes:', missingIds);
        alert(
            `Warning: Loaded file references ${missingIds.length} node(s) that are missing from the Nodes dictionary.\n\n` +
            `The graph will render with placeholders.\n\n` +
            `Missing IDs (first ${Math.min(20, missingIds.length)}): ${preview}`
        );
    }

    private enableControls() {
        const ids = ['tree-select', 'search-input', 'btn-reset', 'btn-save-config', 'btn-load-config', 'btn-edit-config', 'btn-download-mermaid'];
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

    private openConfigModal() {
        if (!this.data) return;
        const modal = document.getElementById('config-modal');
        const graphDirectionSelect = document.getElementById('graph-direction-select') as HTMLSelectElement | null;
        if (!modal || !graphDirectionSelect) return;
        graphDirectionSelect.value = this.graphDirection;
        this.applySubgraphControlsToModal();
        this.renderSubgraphColorLists();
        modal.classList.add('open');
    }

    private closeConfigModal() {
        const modal = document.getElementById('config-modal');
        if (!modal) return;
        modal.classList.remove('open');
    }

    private applyConfigModal() {
        const graphDirectionSelect = document.getElementById('graph-direction-select') as HTMLSelectElement | null;
        if (!graphDirectionSelect) return;
        const nextGraphDirection = graphDirectionSelect.value as GraphDirection;

        const nextSolution = this.readSubgraphControlsFromModal('solution');
        const nextProject = this.readSubgraphControlsFromModal('project');
        const nextFolder = this.readSubgraphControlsFromModal('folder');
        const nextType = this.readSubgraphControlsFromModal('type');
        if (!nextSolution || !nextProject || !nextFolder || !nextType) return;

        const visibilityChanged = (
            nextSolution.visible !== this.subgraphSettings.solution.visible ||
            nextProject.visible !== this.subgraphSettings.project.visible ||
            nextFolder.visible !== this.subgraphSettings.folder.visible ||
            nextType.visible !== this.subgraphSettings.type.visible
        );
        const directionChanged = (
            nextGraphDirection !== this.graphDirection ||
            nextSolution.direction !== this.subgraphSettings.solution.direction ||
            nextProject.direction !== this.subgraphSettings.project.direction ||
            nextFolder.direction !== this.subgraphSettings.folder.direction ||
            nextType.direction !== this.subgraphSettings.type.direction
        );

        this.graphDirection = nextGraphDirection;
        this.subgraphSettings.solution.visible = nextSolution.visible;
        this.subgraphSettings.solution.direction = nextSolution.direction;
        this.subgraphSettings.project.visible = nextProject.visible;
        this.subgraphSettings.project.direction = nextProject.direction;
        this.subgraphSettings.folder.visible = nextFolder.visible;
        this.subgraphSettings.folder.direction = nextFolder.direction;
        this.subgraphSettings.type.visible = nextType.visible;
        this.subgraphSettings.type.direction = nextType.direction;

        const colorsChanged = (
            this.applySubgraphColorsFromModal('solution') ||
            this.applySubgraphColorsFromModal('project') ||
            this.applySubgraphColorsFromModal('folder') ||
            this.applySubgraphColorsFromModal('type')
        );

        this.closeConfigModal();
        if (directionChanged || colorsChanged) {
            this.renderGraph();
            return;
        }
        if (visibilityChanged) {
            this.applySubgraphVisibility();
        }
    }

    private applySubgraphControlsToModal() {
        this.setSubgraphControlValues('solution');
        this.setSubgraphControlValues('project');
        this.setSubgraphControlValues('folder');
        this.setSubgraphControlValues('type');
    }

    private setSubgraphControlValues(level: SubgraphLevel) {
        const visibleInput = document.getElementById(`subgraph-${level}-visible`) as HTMLInputElement | null;
        const directionSelect = document.getElementById(`subgraph-${level}-direction`) as HTMLSelectElement | null;
        if (visibleInput) visibleInput.checked = this.subgraphSettings[level].visible;
        if (directionSelect) directionSelect.value = this.subgraphSettings[level].direction;
    }

    private readSubgraphControlsFromModal(level: SubgraphLevel): { visible: boolean; direction: GraphDirection } | null {
        const visibleInput = document.getElementById(`subgraph-${level}-visible`) as HTMLInputElement | null;
        const directionSelect = document.getElementById(`subgraph-${level}-direction`) as HTMLSelectElement | null;
        if (!visibleInput || !directionSelect) return null;
        return {
            visible: visibleInput.checked,
            direction: directionSelect.value as GraphDirection
        };
    }

    private applySubgraphColorsFromModal(level: SubgraphLevel): boolean {
        if (level === 'folder' || level === 'type') return false;
        const list = document.getElementById(`subgraph-colors-${level}`);
        if (!list) return false;
        const inputs = Array.from(list.querySelectorAll('input[type="color"]')) as HTMLInputElement[];
        const map: Record<string, string> = { ...(this.subgraphSettings[level].colors || {}) };
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

    private renderSubgraphColorLists() {
        this.renderSubgraphColorList('solution');
        this.renderSubgraphColorList('project');
        this.renderSubgraphColorList('folder');
        this.renderSubgraphColorList('type');
    }

    private renderSubgraphColorList(level: SubgraphLevel) {
        const list = document.getElementById(`subgraph-colors-${level}`);
        if (!list) return;
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

    private getSubgraphGroups(level: SubgraphLevel): string[] {
        if (!this.data) return [];
        const treesToRender = (this.activeTreeId === 'all')
            ? this.data.Trees
            : this.data.Trees.filter(t => t.Id === this.activeTreeId);
        const ids = this.collectTreeNodeIds(treesToRender);
        const groups = new Set<string>();

        ids.forEach(id => {
            const node = this.data!.Nodes[id];
            if (!node) return;
            const group = this.getSubgraphGroup(node, level);
            groups.add(group);
        });

        return Array.from(groups).sort((a, b) => a.localeCompare(b));
    }

    private collectTreeNodeIds(trees: ScribeTreeNode[]): Set<string> {
        const ids = new Set<string>();
        const traverse = (node: ScribeTreeNode) => {
            ids.add(node.Id);
            node.ChildNodes.forEach(child => traverse(child));
        };
        trees.forEach(tree => traverse(tree));
        return ids;
    }

    private getSubgraphGroup(node: ScribeNodeData, level: SubgraphLevel): string {
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

    private normalizeGroupName(name: string | undefined | null): string {
        const trimmed = name?.trim();
        return trimmed && trimmed.length > 0 ? trimmed : '(Unknown)';
    }

    private getFirstLevelFolder(meta: MetaInfo): string {
        const path = (meta.DocumentPath || '').replace(/\\/g, '/');
        const segments = path.split('/').filter(Boolean);
        if (segments.length === 0) return '(Root)';

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

    private getDefaultSubgraphColor(group: string): string {
        const hash = this.hashString(group);
        return this.subgraphPalette[hash % this.subgraphPalette.length];
    }

    private getSubgraphColor(level: SubgraphLevel, group: string): string {
        if (level === 'folder') return this.folderSubgraphColor;
        if (level === 'type') return this.typeSubgraphColor;
        const map = this.subgraphSettings[level].colors || {};
        if (map[group]) return map[group];
        const color = this.getDefaultSubgraphColor(group);
        map[group] = color;
        this.subgraphSettings[level].colors = map;
        return color;
    }

    private hashString(value: string): number {
        let hash = 0;
        for (let i = 0; i < value.length; i++) {
            hash = ((hash << 5) - hash) + value.charCodeAt(i);
            hash |= 0;
        }
        return Math.abs(hash);
    }

    private getSubgraphLevelClass(level: SubgraphLevel): string {
        return `sg-level-${level}`;
    }

    private getSubgraphLabelPrefix(level: SubgraphLevel): string {
        if (level === 'solution') return 'Solution';
        if (level === 'project') return 'Project';
        if (level === 'folder') return 'Folder';
        return 'Type';
    }

    private buildSubgraphHierarchy(nodesToRender: Set<string>): Map<string, Map<string, Map<string, Map<string, string[]>>>> {
        const hierarchy = new Map<string, Map<string, Map<string, Map<string, string[]>>>>();
        if (!this.data) return hierarchy;

        nodesToRender.forEach(id => {
            const node = this.data!.Nodes[id];
            if (!node) return;
            const solution = this.getSubgraphGroup(node, 'solution');
            const project = this.getSubgraphGroup(node, 'project');
            const folder = this.getSubgraphGroup(node, 'folder');
            const type = this.getSubgraphGroup(node, 'type');

            let projectMap = hierarchy.get(solution);
            if (!projectMap) {
                projectMap = new Map<string, Map<string, Map<string, string[]>>>();
                hierarchy.set(solution, projectMap);
            }

            let folderMap = projectMap.get(project);
            if (!folderMap) {
                folderMap = new Map<string, Map<string, string[]>>();
                projectMap.set(project, folderMap);
            }

            let typeMap = folderMap.get(folder);
            if (!typeMap) {
                typeMap = new Map<string, string[]>();
                folderMap.set(folder, typeMap);
            }

            const list = typeMap.get(type);
            if (list) {
                list.push(id);
            } else {
                typeMap.set(type, [id]);
            }
        });

        return hierarchy;
    }

    private buildSubgraphModels(hierarchy: Map<string, Map<string, Map<string, Map<string, string[]>>>>): { subgraphs: GraphSubgraph[]; classDefs: GraphClassDef[] } {
        const subgraphs: GraphSubgraph[] = [];
        const classDefs: GraphClassDef[] = [];
        const definedClasses = new Set<string>();

        const createSubgraph = (level: SubgraphLevel, group: string, key: string, nodeIds: string[], children: GraphSubgraph[]): GraphSubgraph => {
            const label = `${this.getSubgraphLabelPrefix(level)}: ${group}`;
            const className = `sg_${level}_${this.hashString(key)}`;
            const color = this.getSubgraphColor(level, group);
            const styles: Record<string, string> = {
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
            const projectSubgraphs: GraphSubgraph[] = [];

            projectEntries.forEach(([projectName, folderMap]) => {
                const folderEntries = Array.from(folderMap.entries()).sort((a, b) => a[0].localeCompare(b[0]));
                const folderSubgraphs: GraphSubgraph[] = [];

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

        const model: GraphModel = {
            direction: this.graphDirection,
            nodes: [],
            edges: [],
            subgraphs: [],
            classDefs: []
        };

        // We need to track processed nodes to handle DAG/Deduplication visually
        const processedNodes = new Set<string>();
        const nodeDecorations = new Map<string, NodeDecoration>();

        // Helper to generate node model
        const generateNodeDefinition = (treeNode: ScribeTreeNode) => {
            const id = treeNode.Id;
            const data = this.data!.Nodes[id];
            const guide = data.Guides;
            const nodeLevel = this.getNodeLevel(id);
            const directHigherChildren = treeNode.ChildNodes.filter(child => this.getNodeLevel(child.Id) > nodeLevel);
            const hasDirectHigherHidden = directHigherChildren.some(child => !nodesToRender.has(child.Id));
            const hasDirectHigherVisible = directHigherChildren.some(child => nodesToRender.has(child.Id));
            
            // Build Node Label
            const labelText = guide?.T || data.MetaInfo?.MemberName || "Unknown";
            
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

            // Use data attributes for event delegation
            const canExpand = hasDirectHigherHidden;

            const canRetract = hasDirectHigherVisible;

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

        const subgraphHierarchy = this.buildSubgraphHierarchy(nodesToRender);
        const subgraphModels = this.buildSubgraphModels(subgraphHierarchy);
        model.subgraphs = subgraphModels.subgraphs;
        model.classDefs.push(...subgraphModels.classDefs);
        this.cacheSubgraphIdsByLevel(model.subgraphs);

        const edgeSet = new Set<string>();
        for (const edge of edges) {
            const key = `${edge.from}-->${edge.to}`;
            if (!edgeSet.has(key)) {
                edgeSet.add(key);
                model.edges.push(edge);
            }
        }

        // Add Style Definitions (Dynamic)
        model.classDefs.push(
            {
                name: 'default',
                styles: { fill: '#e3f2fd', stroke: '#333', 'stroke-width': '1px' }
            },
            {
                name: 'highlighted',
                styles: { stroke: '#ff9800', 'stroke-width': '3px' }
            },
            {
                name: 'tagwarning',
                styles: { fill: '#fff3e0', stroke: '#ffb74d' }
            },
            {
                name: 'tagerror',
                styles: { fill: '#ffebee', stroke: '#ef5350' }
            }
        );

        // Render
        try {
            await this.renderer.render(container, model, nodeDecorations);
            this.applySubgraphVisibility();
        } catch (e) {
            console.error("Graph Render Error", e);
            container.innerText = "Error rendering graph.";
        } finally {
            this.showLoading(false);
        }
    }

    private downloadMermaidDefinition() {
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

    private applySubgraphVisibility() {
        const container = document.getElementById('mermaid-output');
        const svg = container?.querySelector('svg');
        if (!svg) return;

        const levels: SubgraphLevel[] = ['solution', 'project', 'folder', 'type'];
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
                    .find((element): element is SVGGElement => !!element);
                if (!cluster) return;
                if (this.subgraphSettings[level].visible) {
                    cluster.classList.remove('sg-hidden');
                } else {
                    cluster.classList.add('sg-hidden');
                }
            });
        });
    }

    private subgraphIdsByLevel: Record<SubgraphLevel, string[]> = {
        solution: [],
        project: [],
        folder: [],
        type: []
    };

    private cacheSubgraphIdsByLevel(subgraphs: GraphSubgraph[]) {
        const idsByLevel: Record<SubgraphLevel, string[]> = {
            solution: [],
            project: [],
            folder: [],
            type: []
        };

        const visit = (subgraph: GraphSubgraph) => {
            const level = this.getSubgraphLevelFromId(subgraph.id);
            if (level) {
                idsByLevel[level].push(subgraph.id);
            }
            subgraph.subgraphs?.forEach(child => visit(child));
        };

        subgraphs.forEach(subgraph => visit(subgraph));
        this.subgraphIdsByLevel = idsByLevel;
    }

    private getSubgraphLevelFromId(id: string): SubgraphLevel | null {
        if (id.startsWith('solution_')) return 'solution';
        if (id.startsWith('project_')) return 'project';
        if (id.startsWith('folder_')) return 'folder';
        if (id.startsWith('type_')) return 'type';
        return null;
    }

    private renderMetaRow(label: string, value: string | number | null | undefined): string {
        const isEmpty = value === undefined || value === null || value === '';
        const displayValue = isEmpty ? '(empty)' : value;
        return `<div class="meta-item"><span class="meta-label">${label}</span><span class="meta-value">${displayValue}</span></div>`;
    }

    private renderMetaSection(title: string, rows: string[]): string {
        return `<div class="meta-section"><h4 class="meta-section-title">${title}</h4><hr class="meta-section-divider">${rows.join('')}</div>`;
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
        this.renderer.focusNode(id);
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
            if (config.graphDirection) {
                this.graphDirection = config.graphDirection;
            }
            if (config.subgraphs) {
                const applySubgraphConfig = (level: SubgraphLevel, subgraph?: ViewSubgraphConfig) => {
                    if (!subgraph) return;
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
