/// <reference path="types.ts" />

declare var mermaid: any;
declare var svgPanZoom: any;

type GraphDirection = 'TD' | 'BT' | 'LR' | 'RL';
type GraphNode = {
    id: string;
    label: string;
    classes: string[];
};
type GraphEdge = {
    from: string;
    to: string;
};
type GraphClassDef = {
    name: string;
    styles: Record<string, string>;
};
type GraphSubgraph = {
    id: string;
    label: string;
    nodeIds: string[];
    classNames: string[];
    styles: Record<string, string>;
    direction: GraphDirection;
    subgraphs?: GraphSubgraph[];
};
type GraphModel = {
    direction: GraphDirection;
    nodes: GraphNode[];
    edges: GraphEdge[];
    subgraphs: GraphSubgraph[];
    classDefs: GraphClassDef[];
};

type NodeDecoration = {
    id: string;
    className: string;
    hiddenCount: number;
    canExpand: boolean;
    canCollapse: boolean;
};

interface GraphRenderer {
    render(container: HTMLElement, model: GraphModel, decorations: Map<string, NodeDecoration>): Promise<void>;
    focusNode(nodeId: string): void;
}

const SVG_NS = 'http://www.w3.org/2000/svg';

function sanitizeClassName(value: string): string {
    return value.replace(/[^a-zA-Z0-9_-]/g, '_');
}

function getNodeKeyClass(id: string): string {
    return `node-key-${sanitizeClassName(id)}`;
}

class MermaidRenderer implements GraphRenderer {
    private container: HTMLElement | null = null;
    private panZoomInstance: any = null;

    constructor() {
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

    public async render(container: HTMLElement, model: GraphModel, decorations: Map<string, NodeDecoration>): Promise<void> {
        this.container = container;
        if (this.panZoomInstance && this.panZoomInstance.destroy) {
            this.panZoomInstance.destroy();
            this.panZoomInstance = null;
        }

        const graphDef = this.buildGraphDefinition(model);
        let isValid = false;
        try {
            isValid = await mermaid.parse(graphDef);
        } catch (error) {
            this.logGraphDefinition(graphDef);
            throw error;
        }
        if (!isValid) {
            this.logGraphDefinition(graphDef);
            throw new Error("Graph parsing failed");
        }

        const { svg } = await mermaid.render('graphDiv', graphDef);
        container.innerHTML = svg;

        const svgEl = container.querySelector('svg');
        if (svgEl) {
            svgEl.setAttribute('width', '100%');
            svgEl.setAttribute('height', '100%');
            (svgEl as SVGElement).style.width = '100%';
            (svgEl as SVGElement).style.height = '100%';
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

    public focusNode(nodeId: string) {
        if (!this.container || !this.panZoomInstance) return;
        const nodeClass = getNodeKeyClass(nodeId);
        const el = this.container.querySelector(`svg g.node.${nodeClass}`) as SVGGElement | null;
        if (!el) return;

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

    private sanitizeMermaidLabel(value: string): string {
        return value.replace(/[\r\n]+/g, ' ').replace(/"/g, '\\"');
    }

    private serializeStyles(styles: Record<string, string>): string {
        return Object.entries(styles)
            .map(([key, value]) => `${key}:${value}`)
            .join(',');
    }

    private buildGraphDefinition(model: GraphModel): string {
        let graphDef = `flowchart ${this.getMermaidDirection(model.direction)}\n`;
        const subgraphStyleLines: string[] = [];

        model.nodes.forEach(node => {
            const cleanLabel = this.sanitizeMermaidLabel(node.label);
            graphDef += `    ${node.id}["${cleanLabel}"]\n`;
            if (node.classes.length > 0) {
                graphDef += `    class ${node.id} ${node.classes.join(',')}\n`;
            }
        });

        const appendSubgraph = (subgraph: GraphSubgraph, indent: string) => {
            const cleanLabel = this.sanitizeMermaidLabel(subgraph.label);
            graphDef += `${indent}subgraph ${subgraph.id}["${cleanLabel}"]\n`;
            graphDef += `${indent}    direction ${this.getMermaidDirection(subgraph.direction)}\n`;
            subgraph.subgraphs?.forEach(child => {
                appendSubgraph(child, `${indent}    `);
            });
            subgraph.nodeIds.forEach(id => {
                graphDef += `${indent}    ${id}\n`;
            });
            graphDef += `${indent}end\n`;
            if (subgraph.classNames.length > 0) {
                subgraphStyleLines.push(`    class ${subgraph.id} ${subgraph.classNames.join(',')}`);
            }
            subgraphStyleLines.push(`    style ${subgraph.id} ${this.serializeStyles(subgraph.styles)}`);
        };

        model.subgraphs.forEach(subgraph => {
            appendSubgraph(subgraph, '    ');
        });

        model.edges.forEach(edge => {
            graphDef += `    ${edge.from} --> ${edge.to}\n`;
        });

        subgraphStyleLines.forEach(line => {
            graphDef += `${line}\n`;
        });

        graphDef += '\n';
        model.classDefs.forEach(classDef => {
            graphDef += `    classDef ${classDef.name} ${this.serializeStyles(classDef.styles)};\n`;
        });

        return graphDef;
    }

    private logGraphDefinition(graphDef: string) {
        const lines = graphDef.split(/\r?\n/);
        const numbered = lines
            .map((line, index) => `${String(index + 1).padStart(4, ' ')}| ${line}`)
            .join('\n');
        console.error('Mermaid definition:\n' + numbered);
        if (typeof window !== 'undefined') {
            (window as any).lastMermaidGraphDef = numbered;
        }
    }

    private getMermaidDirection(direction: GraphDirection): 'TB' | 'BT' | 'LR' | 'RL' {
        if (direction === 'TD') return 'TB';
        return direction;
    }

    private createSvgElement<T extends keyof SVGElementTagNameMap>(tag: T): SVGElementTagNameMap[T] {
        return document.createElementNS(SVG_NS, tag);
    }

    private buildIcon(icon: 'expand' | 'collapse' | 'details'): SVGGElement {
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
        } else if (icon === 'collapse') {
            const hLine = this.createSvgElement('line');
            hLine.setAttribute('x1', '5');
            hLine.setAttribute('y1', '12');
            hLine.setAttribute('x2', '19');
            hLine.setAttribute('y2', '12');
            iconGroup.appendChild(hLine);
        } else {
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

    private appendIconButton(parent: SVGGElement, x: number, y: number, size: number, icon: 'expand' | 'collapse' | 'details', action: string, nodeId: string, title: string) {
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

    private appendBadge(parent: SVGGElement, x: number, y: number, count: number) {
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

    private hydrateNodeDecorations(container: HTMLElement, nodeDecorations: Map<string, NodeDecoration>) {
        const svg = container.querySelector('svg');
        if (!svg) return;

        nodeDecorations.forEach(decoration => {
            const nodeEl = svg.querySelector(`g.node.${decoration.className}`) as SVGGElement | null;
            if (!nodeEl) return;

            const bbox = nodeEl.getBBox();
            const rect = nodeEl.querySelector('rect.label-container') as SVGRectElement | null;
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

            const buttons: Array<{ action: string; icon: 'expand' | 'collapse' | 'details'; title: string }> = [];
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
