const views = new WeakMap();
const baseScale = 48;

export function initialize(canvas, dotnet, mesh) {
    const view = {
        canvas, dotnet, zoom: 1, ox: 0, oy: 0, dragging: false, pointerId: null, x: 0, y: 0,
        moved: false, selections: new Map(), hover: null, message: null, showSuperStructure: true,
        canUndo: false, canRedo: false, tool: "select", constraintStart: null,
        readOnly: false, quadVertices: [], quads: [], quadTriangles: [], quadEdgeFlips: 0,
        polylineNodes: [], polygonNodes: [],
        renderPending: false, reportTimer: null, hoverLookupTimer: null, hoverPoint: null,
        hoverLookupVersion: 0,
        hoverLine: [], hoverRequest: 0,
        vertices: [], nodeKinds: [], triangles: [], boundaryEdges: [], loopEdges: [], constraintEdges: [],
        superNodes: new Set(), superFaces: new Set(), constraintEdgeIds: new Set(),
        constraintByEdge: new Map(), constraints: [], faceKinds: []
    };
    applyMesh(view, mesh);
    views.set(canvas, view);

    view.resize = new ResizeObserver(() => { sizeCanvas(view); draw(view); });
    view.resize.observe(canvas);
    view.wheel = event => onWheel(view, event);
    view.down = event => onPointerDown(view, event);
    view.move = event => onPointerMove(view, event);
    view.up = event => onPointerUp(view, event);
    view.leave = () => {
        view.hoverLookupVersion++;
        view.hoverPoint = null;
        setHover(view, null);
    };
    view.key = event => onKey(view, event);
    view.contextMenu = event => deleteNode(view, event);
    canvas.addEventListener("wheel", view.wheel, { passive: false });
    canvas.addEventListener("pointerdown", view.down);
    canvas.addEventListener("pointermove", view.move);
    canvas.addEventListener("pointerup", view.up);
    canvas.addEventListener("pointercancel", view.up);
    canvas.addEventListener("pointerleave", view.leave);
    canvas.addEventListener("keydown", view.key);
    canvas.addEventListener("contextmenu", view.contextMenu);
    sizeCanvas(view);
    fit(canvas);
}

export function reset(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    view.zoom = 1;
    view.ox = view.width / 2;
    view.oy = view.height / 2;
    changed(view);
}

export function fit(canvas) {
    const view = views.get(canvas);
    if (!view || !view.width || !view.height) return;
    const activeVertices = view.vertices.filter((vertex, id) =>
        vertex && (view.showSuperStructure || !view.superNodes.has(id)));
    const xs = activeVertices.map(v => v[0]);
    const ys = activeVertices.map(v => v[1]);
    const minX = Math.min(...xs), maxX = Math.max(...xs), minY = Math.min(...ys), maxY = Math.max(...ys);
    const padding = Math.min(90, Math.min(view.width, view.height) * .15);
    view.zoom = clamp(Math.min((view.width - padding * 2) / ((maxX - minX) * baseScale),
                               (view.height - padding * 2) / ((maxY - minY) * baseScale)), .05, 100);
    const scale = baseScale * view.zoom;
    view.ox = view.width / 2 - ((minX + maxX) / 2) * scale;
    view.oy = view.height / 2 + ((minY + maxY) / 2) * scale;
    changed(view);
}

export async function resetMesh(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    applyMesh(view, await view.dotnet.invokeMethodAsync("ResetMeshState"));
    view.selections.clear();
    view.hover = null;
    view.message = null;
    fit(canvas);
}

export function setSuperStructureVisibility(canvas, visible) {
    const view = views.get(canvas);
    if (!view) return;
    view.showSuperStructure = visible;
    view.selections.clear();
    view.hover = null;
    fit(canvas);
}

export function setToolMode(canvas, tool) {
    const view = views.get(canvas);
    if (!view || (view.readOnly && tool !== "select")) return;
    view.tool = tool;
    view.constraintStart = null;
    view.polylineNodes = [];
    view.polygonNodes = [];
    view.selections.clear();
    view.message = null;
    changed(view);
}

export function showQuadMesh(canvas, overlay) {
    const view = views.get(canvas);
    if (!view) return;
    view.readOnly = true;
    view.tool = "select";
    view.constraintStart = null;
    view.polylineNodes = [];
    view.polygonNodes = [];
    view.quadVertices = overlay.nodes.map(node => [node.x, node.y]);
    view.quads = overlay.quads.map(face => [face.a, face.b, face.c, face.d]);
    view.quadTriangles = overlay.triangles.map(face => [face.a, face.b, face.c]);
    view.quadEdgeFlips = overlay.edgeFlips;
    view.message = `${view.quads.length} quads · ${view.quadTriangles.length} remaining triangles · ${view.quadEdgeFlips} flips`;
    changed(view);
}

export function hideQuadMesh(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    view.readOnly = false;
    view.quadVertices = [];
    view.quads = [];
    view.quadTriangles = [];
    view.quadEdgeFlips = 0;
    view.message = null;
    changed(view);
}

export async function undo(canvas) {
    const view = views.get(canvas);
    if (!view || view.readOnly || !view.canUndo) return;
    applyMesh(view, await view.dotnet.invokeMethodAsync("UndoAction"));
    view.selections.clear();
    view.hover = null;
    view.message = null;
    changed(view);
}

export async function redo(canvas) {
    const view = views.get(canvas);
    if (!view || view.readOnly || !view.canRedo) return;
    applyMesh(view, await view.dotnet.invokeMethodAsync("RedoAction"));
    view.selections.clear();
    view.hover = null;
    view.message = null;
    changed(view);
}

export function dispose(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    view.resize.disconnect();
    canvas.removeEventListener("wheel", view.wheel);
    canvas.removeEventListener("pointerdown", view.down);
    canvas.removeEventListener("pointermove", view.move);
    canvas.removeEventListener("pointerup", view.up);
    canvas.removeEventListener("pointercancel", view.up);
    canvas.removeEventListener("pointerleave", view.leave);
    canvas.removeEventListener("keydown", view.key);
    canvas.removeEventListener("contextmenu", view.contextMenu);
    if (view.reportTimer !== null) clearTimeout(view.reportTimer);
    if (view.hoverLookupTimer !== null) clearTimeout(view.hoverLookupTimer);
    views.delete(canvas);
}

function onWheel(view, event) {
    event.preventDefault();
    const p = localPoint(view.canvas, event);
    zoomAround(view, p.x, p.y, Math.exp(-event.deltaY * .0015));
}

function onPointerDown(view, event) {
    if (event.button !== 0 && event.button !== 1) return;
    event.preventDefault();
    view.canvas.focus();
    view.dragging = event.button === 1;
    view.selecting = event.button === 0;
    view.pointerId = event.pointerId;
    view.lastX = event.clientX;
    view.lastY = event.clientY;
    view.downX = event.clientX;
    view.downY = event.clientY;
    view.moved = false;
    view.canvas.setPointerCapture(event.pointerId);
    if (view.dragging) view.canvas.classList.add("dragging");
}

function onPointerMove(view, event) {
    const p = localPoint(view.canvas, event);
    const world = screenToWorld(view, p.x, p.y);
    view.x = world.x;
    view.y = world.y;
    if (event.pointerId === view.pointerId &&
        (event.clientX - view.downX) ** 2 + (event.clientY - view.downY) ** 2 > 16) view.moved = true;
    if (view.dragging && event.pointerId === view.pointerId) {
        view.hoverLookupVersion++;
        view.hoverPoint = null;
        setHover(view, null);
        view.ox += event.clientX - view.lastX;
        view.oy += event.clientY - view.lastY;
        view.lastX = event.clientX;
        view.lastY = event.clientY;
        draw(view);
    } else {
        scheduleMeshHover(view, p);
    }
    if (view.polylineNodes.length || view.polygonNodes.length) requestDraw(view);
    scheduleReport(view);
}

function onPointerUp(view, event) {
    if (event.pointerId !== view.pointerId) return;
    const shouldSelect = view.selecting && !view.moved;
    view.dragging = false;
    view.selecting = false;
    view.pointerId = null;
    view.canvas.classList.remove("dragging");
    if (shouldSelect && !view.readOnly && view.tool === "constraint") chooseConstraintNode(view, event);
    else if (shouldSelect && view.tool === "polyline") choosePolylineNode(view, event);
    else if (shouldSelect && view.tool === "polygon") choosePolygonNode(view, event);
    else if (shouldSelect && event.metaKey && !view.readOnly) insertNode(view, event);
    else if (shouldSelect) selectAt(view, localPoint(view.canvas, event), event);
}

function onKey(view, event) {
    if (!view.readOnly && event.metaKey && event.key.toLowerCase() === "z" && event.shiftKey) redo(view.canvas);
    else if (!view.readOnly && event.metaKey && event.key.toLowerCase() === "z") undo(view.canvas);
    else if (event.key === "0") reset(view.canvas);
    else if (event.key.toLowerCase() === "f") fit(view.canvas);
    else if (event.key === "Enter" && view.tool === "polyline") finishPolyline(view);
    else if (event.key === "Enter" && view.tool === "polygon") finishPolygon(view);
    else if (event.key === "Backspace" && view.tool === "polyline" && view.polylineNodes.length) {
        view.polylineNodes.pop();
        view.selections.clear();
        for (const id of view.polylineNodes) addSelection(view, { type: "node", id });
        view.message = view.polylineNodes.length ? `${view.polylineNodes.length} polyline points — Enter to finish` : null;
        changed(view);
    }
    else if (event.key === "Backspace" && view.tool === "polygon" && view.polygonNodes.length) {
        view.polygonNodes.pop();
        syncDraftSelection(view, view.polygonNodes);
        view.message = view.polygonNodes.length ? `${view.polygonNodes.length} polygon vertices — Enter to close` : null;
        changed(view);
    }
    else if (!view.readOnly && (event.key === "Delete" || event.key === "Backspace")) deleteSelected(view);
    else if (event.key === "Escape") {
        view.tool = "select";
        view.constraintStart = null;
        view.polylineNodes = [];
        view.polygonNodes = [];
        view.selections.clear();
        view.message = null;
        view.dotnet.invokeMethodAsync("SelectDefaultTool");
        changed(view);
    }
    else if (event.key === "+" || event.key === "=") zoomAround(view, view.width / 2, view.height / 2, 1.2);
    else if (event.key === "-" || event.key === "_") zoomAround(view, view.width / 2, view.height / 2, 1 / 1.2);
    else return;
    event.preventDefault();
}

async function deleteSelected(view) {
    if (view.readOnly) return;
    const selected = [...view.selections.values()];
    let changedMesh = false;
    const constraints = selected
        .filter(item => item.type === "constraint")
        .sort((left, right) => right.id - left.id);
    for (const item of constraints) {
        const mesh = await view.dotnet.invokeMethodAsync("RemoveConstraintLine", item.id);
        if (mesh.succeeded) { applyMesh(view, mesh); changedMesh = true; }
    }
    for (const item of selected.filter(item => item.type === "node")) {
        const mesh = await view.dotnet.invokeMethodAsync("RemoveNode", item.id);
        if (mesh.succeeded) { applyMesh(view, mesh); changedMesh = true; }
    }
    view.selections.clear();
    view.hover = null;
    view.hoverLine = [];
    view.message = changedMesh ? null : "Selected elements cannot be deleted";
    changed(view);
}

async function insertNode(view, event) {
    if (view.readOnly) return;
    const screen = localPoint(view.canvas, event);
    const existing = await resolveHit(view, screen);
    if (existing?.type === "node") {
        view.selections.clear();
        addSelection(view, existing);
        changed(view);
        return;
    }
    const point = screenToWorld(view, screen.x, screen.y);
    const mesh = await view.dotnet.invokeMethodAsync("InsertNode", point.x, point.y);
    if (!mesh.succeeded || mesh.changedNodeId === null) return;
    applyMesh(view, mesh);
    view.selections.clear();
    addSelection(view, { type: "node", id: mesh.changedNodeId });
    view.hover = null;
    view.message = null;
    changed(view);
}

async function deleteNode(view, event) {
    event.preventDefault();
    if (view.readOnly) return;
    const screen = localPoint(view.canvas, event);
    const hit = await resolveHit(view, screen);
    if (hit?.type !== "node") return;
    const mesh = await view.dotnet.invokeMethodAsync("RemoveNode", hit.id);
    if (!mesh.succeeded) {
        view.message = `Node ${hit.id} cannot be removed`;
        report(view);
        return;
    }
    applyMesh(view, mesh);
    view.selections.clear();
    view.hover = null;
    view.message = null;
    changed(view);
}

function applyMesh(view, mesh) {
    view.hoverLookupVersion++;
    view.hoverPoint = null;
    const maxId = mesh.nodes.reduce((maximum, node) => Math.max(maximum, node.id), -1);
    view.vertices = Array(maxId + 1).fill(null);
    view.nodeKinds = Array(maxId + 1).fill("Normal");
    for (const node of mesh.nodes) {
        view.vertices[node.id] = [node.x, node.y];
        view.nodeKinds[node.id] = node.kind;
    }
    view.triangles = mesh.faces.map(face => [face.a, face.b, face.c]);
    view.faceKinds = mesh.faces.map(face => face.kind);
    view.boundaryEdges = mesh.boundaryEdges.map(edge => [edge.a, edge.b]);
    view.loopEdges = mesh.loopEdges.map(edge => [edge.a, edge.b]);
    view.constraintEdges = mesh.constraintEdges.map(edge => [edge.a, edge.b]);
    view.constraintEdgeIds = new Set(view.constraintEdges.map(edge => edgeId(edge[0], edge[1])));
    view.constraints = mesh.constraints;
    view.constraintByEdge = new Map();
    for (const constraint of mesh.constraints) {
        for (const edge of constraint.edges)
            view.constraintByEdge.set(edgeId(edge.a, edge.b), constraint);
    }
    view.superNodes = new Set(mesh.nodes.filter(node => node.isSuper).map(node => node.id));
    view.superFaces = new Set(mesh.faces.map((face, index) => face.isSuper ? index : -1).filter(index => index >= 0));
    view.canUndo = mesh.canUndo;
    view.canRedo = mesh.canRedo;
}

async function selectAt(view, screen, event) {
    view.message = null;
    const hit = await resolveHit(view, screen);
    if (hit?.type === "constraint") {
        const edges = await view.dotnet.invokeMethodAsync("CollectConstraintLine", hit.a, hit.b);
        hit.edges = edges.map(edge => [edge.a, edge.b]);
    }
    if (!hit) {
        if (!event.shiftKey && !event.ctrlKey) view.selections.clear();
    } else {
        const key = selectionKey(hit);
        if (event.ctrlKey) {
            if (view.selections.has(key)) view.selections.delete(key);
            else view.selections.set(key, hit);
        } else if (event.shiftKey) {
            view.selections.set(key, hit);
        } else if (view.selections.size === 1 && view.selections.has(key)) {
            view.selections.clear();
        } else {
            view.selections.clear();
            view.selections.set(key, hit);
        }
    }
    changed(view);
}

function setHover(view, hit) {
    if (selectionKeyOrEmpty(view.hover) === selectionKeyOrEmpty(hit)) return;
    view.hover = hit;
    view.hoverLine = [];
    const request = ++view.hoverRequest;
    draw(view);
    if (hit?.type === "edge" || hit?.type === "constraint") {
        view.dotnet.invokeMethodAsync("CollectConstraintLine", hit.a, hit.b).then(edges => {
            if (request !== view.hoverRequest) return;
            view.hoverLine = edges.map(edge => [edge.a, edge.b]);
            draw(view);
        });
    }
}

async function chooseConstraintNode(view, event) {
    const node = await nodeAtOrInsert(view, event);
    if (!node || view.superNodes.has(node.id)) return;
    if (view.constraintStart === null) {
        view.constraintStart = node.id;
        view.selections.clear();
        addSelection(view, node);
        view.message = "Choose constraint end node";
        changed(view);
        return;
    }
    if (view.constraintStart === node.id) return;
    const mesh = await view.dotnet.invokeMethodAsync(
        "InsertConstraintLine", view.constraintStart, node.id);
    if (!mesh.succeeded) {
        view.message = "Constraint could not be inserted";
        report(view);
        return;
    }
    applyMesh(view, mesh);
    view.constraintStart = null;
    view.selections.clear();
    view.message = null;
    changed(view);
}

async function choosePolylineNode(view, event) {
    const node = await nodeAtOrInsert(view, event);
    if (!node || view.superNodes.has(node.id) || view.polylineNodes.at(-1) === node.id) return;
    view.polylineNodes.push(node.id);
    addSelection(view, node);
    view.message = `${view.polylineNodes.length} polyline points — Enter to finish`;
    changed(view);
}

async function finishPolyline(view) {
    if (view.polylineNodes.length < 2) return;
    const mesh = await view.dotnet.invokeMethodAsync("InsertPolyline", view.polylineNodes);
    if (!mesh.succeeded) {
        view.message = "Polyline could not be inserted";
        report(view);
        return;
    }
    applyMesh(view, mesh);
    view.polylineNodes = [];
    view.selections.clear();
    view.message = null;
    changed(view);
}

async function choosePolygonNode(view, event) {
    const node = await nodeAtOrInsert(view, event);
    if (!node || view.superNodes.has(node.id) || view.polygonNodes.includes(node.id)) return;
    view.polygonNodes.push(node.id);
    addSelection(view, node);
    view.message = `${view.polygonNodes.length} polygon vertices — Enter to close`;
    changed(view);
}

async function finishPolygon(view) {
    if (view.polygonNodes.length < 3) return;
    const mesh = await view.dotnet.invokeMethodAsync("InsertPolygon", view.polygonNodes);
    if (!mesh.succeeded) {
        view.message = "Polygon could not be inserted";
        report(view);
        return;
    }
    applyMesh(view, mesh);
    view.polygonNodes = [];
    view.selections.clear();
    view.message = null;
    changed(view);
}

async function nodeAtOrInsert(view, event) {
    const screen = localPoint(view.canvas, event);
    const hit = await resolveHit(view, screen);
    if (hit?.type === "node") return hit;
    if (!event.metaKey) return null;

    const point = screenToWorld(view, screen.x, screen.y);
    const inserted = await view.dotnet.invokeMethodAsync("InsertNode", point.x, point.y);
    if (!inserted.succeeded || inserted.changedNodeId === null) return null;
    applyMesh(view, inserted);
    view.hover = null;
    return { type: "node", id: inserted.changedNodeId };
}

function syncDraftSelection(view, ids) {
    view.selections.clear();
    for (const id of ids) addSelection(view, { type: "node", id });
}

function scheduleMeshHover(view, screen) {
    view.hoverPoint = screen;
    view.hoverLookupVersion++;
    if (view.hoverLookupTimer !== null) return;
    view.hoverLookupTimer = setTimeout(async () => {
        view.hoverLookupTimer = null;
        const point = view.hoverPoint;
        view.hoverPoint = null;
        const version = view.hoverLookupVersion;
        const hit = await resolveHit(view, point);
        if (version === view.hoverLookupVersion) setHover(view, hit);
    }, 32);
}

async function resolveHit(view, screen) {
    if (!screen) return null;
    const world = screenToWorld(view, screen.x, screen.y);
    const hit = await view.dotnet.invokeMethodAsync(
        "FindElement", world.x, world.y, 9 / (baseScale * view.zoom));
    if (!hit) return null;
    if (hit.type === "node") {
        if (!view.showSuperStructure && view.superNodes.has(hit.id)) return null;
        return { type: "node", id: hit.id };
    }
    if (hit.type === "face") {
        if (!view.showSuperStructure && view.superFaces.has(hit.id)) return null;
        return { type: "face", id: hit.id };
    }
    const a = hit.a, b = hit.b;
    if (!view.showSuperStructure && (view.superNodes.has(a) || view.superNodes.has(b))) return null;
    const id = edgeId(a, b);
    const constraint = view.constraintByEdge.get(id);
    return constraint
        ? { type: "constraint", id: constraint.id, a, b, constraint,
            edges: constraint.edges.map(edge => [edge.a, edge.b]) }
        : { type: "edge", id, a, b };
}

function zoomAround(view, x, y, factor) {
    const world = screenToWorld(view, x, y);
    view.zoom = clamp(view.zoom * factor, .05, 100);
    const scale = baseScale * view.zoom;
    view.ox = x - world.x * scale;
    view.oy = y + world.y * scale;
    changed(view);
}

function sizeCanvas(view) {
    const rect = view.canvas.getBoundingClientRect();
    const ratio = window.devicePixelRatio || 1;
    view.width = rect.width;
    view.height = rect.height;
    view.canvas.width = Math.max(1, Math.round(rect.width * ratio));
    view.canvas.height = Math.max(1, Math.round(rect.height * ratio));
    view.ctx = view.canvas.getContext("2d");
    view.ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
}

function draw(view) {
    const ctx = view.ctx;
    if (!ctx) return;
    ctx.clearRect(0, 0, view.width, view.height);
    ctx.fillStyle = "#080d18";
    ctx.fillRect(0, 0, view.width, view.height);
    drawGrid(view, ctx);
    if (view.readOnly) { ctx.save(); ctx.globalAlpha = .32; }
    drawMesh(view, ctx);
    if (view.readOnly) ctx.restore();
    if (view.readOnly) drawQuadOverlay(view, ctx);
}

function drawQuadOverlay(view, ctx) {
    ctx.save();
    ctx.lineJoin = "round";
    ctx.lineCap = "round";
    for (const quad of view.quads) {
        const points = quad.map(index => worldToScreen(view, view.quadVertices[index]));
        ctx.beginPath();
        ctx.moveTo(points[0].x, points[0].y);
        for (let index = 1; index < points.length; index++) ctx.lineTo(points[index].x, points[index].y);
        ctx.closePath();
        ctx.fillStyle = "rgba(245, 158, 11, .12)";
        ctx.strokeStyle = "#fbbf24";
        ctx.lineWidth = 3;
        ctx.fill();
        ctx.stroke();
    }
    ctx.setLineDash([7, 5]);
    for (const triangle of view.quadTriangles) {
        const points = triangle.map(index => worldToScreen(view, view.quadVertices[index]));
        ctx.beginPath();
        ctx.moveTo(points[0].x, points[0].y);
        ctx.lineTo(points[1].x, points[1].y);
        ctx.lineTo(points[2].x, points[2].y);
        ctx.closePath();
        ctx.fillStyle = "rgba(148, 163, 184, .08)";
        ctx.strokeStyle = "#94a3b8";
        ctx.lineWidth = 2;
        ctx.fill();
        ctx.stroke();
    }
    ctx.restore();
}

function drawGrid(view, ctx) {
    const scale = baseScale * view.zoom;
    const step = niceStep(70 / scale);
    const pixels = step * scale;
    const firstX = Math.floor(-view.ox / pixels), lastX = Math.ceil((view.width - view.ox) / pixels);
    const firstY = Math.floor((view.oy - view.height) / pixels), lastY = Math.ceil(view.oy / pixels);
    ctx.lineWidth = 1;
    for (let i = firstX; i <= lastX; i++) {
        const x = view.ox + i * pixels;
        ctx.strokeStyle = i === 0 ? "#4ade80" : i % 5 === 0 ? "#22314d" : "#142039";
        line(ctx, x, 0, x, view.height);
    }
    for (let i = firstY; i <= lastY; i++) {
        const y = view.oy - i * pixels;
        ctx.strokeStyle = i === 0 ? "#f87171" : i % 5 === 0 ? "#22314d" : "#142039";
        line(ctx, 0, y, view.width, y);
    }
}

function drawMesh(view, ctx) {
    ctx.lineJoin = "round";
    for (let triangleIndex = 0; triangleIndex < view.triangles.length; triangleIndex++) {
        if (!view.showSuperStructure && view.superFaces.has(triangleIndex)) continue;
        const triangle = view.triangles[triangleIndex];
        const points = triangle.map(i => worldToScreen(view, view.vertices[i]));
        ctx.beginPath();
        ctx.moveTo(points[0].x, points[0].y);
        ctx.lineTo(points[1].x, points[1].y);
        ctx.lineTo(points[2].x, points[2].y);
        ctx.closePath();
        const faceSelected = view.selections.has(`face:${triangleIndex}`);
        const faceHovered = view.hover?.type === "face" && view.hover.id === triangleIndex;
        const isSuper = view.superFaces.has(triangleIndex);
        const kind = view.faceKinds[triangleIndex];
        ctx.fillStyle = faceSelected ? "rgba(245, 158, 11, .32)" :
            faceHovered ? "rgba(34, 211, 238, .22)" :
            isSuper ? "rgba(124, 58, 237, .10)" :
            kind === "Island" ? "rgba(34, 197, 94, .20)" :
            kind === "Lake" ? "rgba(14, 165, 233, .28)" : "rgba(30, 41, 59, .38)";
        ctx.fill();
        ctx.strokeStyle = isSuper ? "#6d5ba8" : "#67e8f9";
        ctx.lineWidth = 1.35;
        ctx.stroke();
    }
    ctx.strokeStyle = "#34d399";
    ctx.lineWidth = 2.4;
    for (const [start, end] of view.loopEdges) {
        const a = worldToScreen(view, view.vertices[start]);
        const b = worldToScreen(view, view.vertices[end]);
        line(ctx, a.x, a.y, b.x, b.y);
    }
    ctx.strokeStyle = "#f472b6";
    ctx.lineWidth = 2.4;
    for (const constraint of view.constraints)
        strokeEdgePath(view, ctx, constraint.edges.map(edge => [edge.a, edge.b]));
    if (view.hoverLine.length) {
        ctx.strokeStyle = "#fbbf24";
        ctx.lineWidth = 5;
        strokeEdgePath(view, ctx, view.hoverLine);
    }
    if (view.tool === "polyline" && view.polylineNodes.length) {
        ctx.strokeStyle = "#fbbf24";
        ctx.lineWidth = 2;
        ctx.setLineDash([7, 5]);
        ctx.beginPath();
        view.polylineNodes.forEach((id, index) => {
            const p = worldToScreen(view, view.vertices[id]);
            index ? ctx.lineTo(p.x, p.y) : ctx.moveTo(p.x, p.y);
        });
        const pointer = worldToScreen(view, [view.x, view.y]);
        ctx.lineTo(pointer.x, pointer.y);
        ctx.stroke();
        ctx.setLineDash([]);
    }
    if (view.tool === "polygon" && view.polygonNodes.length) {
        ctx.strokeStyle = "#fbbf24";
        ctx.fillStyle = "rgba(251, 191, 36, .08)";
        ctx.lineWidth = 2;
        ctx.setLineDash([7, 5]);
        ctx.beginPath();
        view.polygonNodes.forEach((id, index) => {
            const p = worldToScreen(view, view.vertices[id]);
            index ? ctx.lineTo(p.x, p.y) : ctx.moveTo(p.x, p.y);
        });
        const pointer = worldToScreen(view, [view.x, view.y]);
        ctx.lineTo(pointer.x, pointer.y);
        if (view.polygonNodes.length > 1) {
            const first = worldToScreen(view, view.vertices[view.polygonNodes[0]]);
            ctx.lineTo(first.x, first.y);
        }
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
        ctx.setLineDash([]);
    }
    if (view.hover?.type === "edge" && !view.selections.has(`edge:${view.hover.id}`)) {
        const a = worldToScreen(view, view.vertices[view.hover.a]);
        const b = worldToScreen(view, view.vertices[view.hover.b]);
        ctx.strokeStyle = "#a5f3fc";
        ctx.lineWidth = 3;
        line(ctx, a.x, a.y, b.x, b.y);
    }
    for (const selectedEdge of [...view.selections.values()].filter(item => item.type === "edge")) {
        const a = worldToScreen(view, view.vertices[selectedEdge.a]);
        const b = worldToScreen(view, view.vertices[selectedEdge.b]);
        ctx.strokeStyle = "#f59e0b";
        ctx.lineWidth = 4;
        line(ctx, a.x, a.y, b.x, b.y);
    }
    for (const constraint of [...view.selections.values()].filter(item => item.type === "constraint")) {
        ctx.strokeStyle = "#f59e0b";
        ctx.lineWidth = 5;
        strokeEdgePath(view, ctx, constraint.edges ?? []);
    }
    if (view.showSuperStructure) {
        ctx.strokeStyle = "#8b74c9";
        ctx.lineWidth = 2.1;
        for (const [start, end] of view.boundaryEdges) {
            const a = worldToScreen(view, view.vertices[start]);
            const b = worldToScreen(view, view.vertices[end]);
            line(ctx, a.x, a.y, b.x, b.y);
        }
    }
    for (let index = 0; index < view.vertices.length; index++) {
        const vertex = view.vertices[index];
        if (!vertex || (!view.showSuperStructure && view.superNodes.has(index))) continue;
        const p = worldToScreen(view, vertex);
        const selected = view.selections.has(`node:${index}`);
        const hovered = view.hover?.type === "node" && view.hover.id === index;
        ctx.fillStyle = selected ? "#f59e0b" : hovered ? "#22d3ee" :
            view.superNodes.has(index) ? "#a78bfa" : "#f8fafc";
        ctx.beginPath(); ctx.arc(p.x, p.y, selected ? 6 : hovered ? 5 : 3.2, 0, Math.PI * 2); ctx.fill();
    }
}

function changed(view) { draw(view); report(view); }
function requestDraw(view) {
    if (view.renderPending) return;
    view.renderPending = true;
    requestAnimationFrame(() => {
        view.renderPending = false;
        draw(view);
    });
}
function scheduleReport(view) {
    if (view.reportTimer !== null) return;
    view.reportTimer = setTimeout(() => {
        view.reportTimer = null;
        report(view);
    }, 50);
}
function report(view) {
    const selected = [...view.selections.values()];
    const selectedObjects = selected.map(item => describeSelection(view, item));
    const selection = view.message || (selected.length === 0 ? "No selection" : selected.length === 1
        ? `${capitalize(selected[0].type)} ${selected[0].id}` : `${selected.length} selected`);
    view.dotnet.invokeMethodAsync(
        "UpdateStatus", view.x, view.y, view.zoom,
        view.vertices.filter((vertex, id) => vertex && !view.superNodes.has(id)).length,
        selection, view.canUndo, view.canRedo, selectedObjects);
}
function describeSelection(view, item) {
    if (item.type === "node") {
        const point = view.vertices[item.id];
        return selectionInfo("Node", `Node ${item.id}`, [
            ["ID", item.id], ["X", number(point[0])], ["Y", number(point[1])],
            ["Kind", view.nodeKinds[item.id]],
            ["Super structure", view.superNodes.has(item.id) ? "Yes" : "No"]
        ]);
    }
    if (item.type === "face") {
        const triangle = view.triangles[item.id];
        const a = view.vertices[triangle[0]], b = view.vertices[triangle[1]], c = view.vertices[triangle[2]];
        const signedArea = ((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])) / 2;
        return selectionInfo("Face", `Face ${item.id}`, [
            ["Nodes", triangle.join(", ")], ["Area", number(Math.abs(signedArea))],
            ["Orientation", signedArea >= 0 ? "CCW" : "CW"],
            ["Classification", view.faceKinds[item.id]],
            ["Super structure", view.superFaces.has(item.id) ? "Yes" : "No"]
        ]);
    }
    const edges = item.type === "constraint" ? item.edges ?? [] : [[item.a, item.b]];
    const length = edges.reduce((total, edge) => total + edgeLength(view, edge[0], edge[1]), 0);
    if (item.type === "constraint") {
        const constraint = item.constraint;
        return selectionInfo("Constraint", constraint?.name ?? `Constraint ${item.id}`, [
            ["ID", constraint?.id ?? item.id], ["Kind", "Feature"],
            ["Spans", constraint?.spanCount ?? 1],
            ["Collected edges", constraint?.segmentCount ?? edges.length],
            ["Length", number(constraint?.length ?? length)],
            ["Start node", constraint?.startNodeId ?? edges[0]?.[0] ?? item.a],
            ["End node", constraint?.endNodeId ?? edges.at(-1)?.[1] ?? item.b]
        ]);
    }
    const kind = view.loopEdges.some(edge => edgeId(edge[0], edge[1]) === item.id)
        ? "Boundary" : view.constraintEdgeIds.has(item.id) ? "Feature" : "Interior";
    return selectionInfo("Edge", `Edge ${item.id}`, [
        ["Start node", item.a], ["End node", item.b], ["Length", number(length)], ["Kind", kind]
    ]);
}
function selectionInfo(type, title, entries) {
    return { type, title, properties: entries.map(entry => ({ name: entry[0], value: String(entry[1]) })) };
}
function edgeLength(view, a, b) {
    const first = view.vertices[a], second = view.vertices[b];
    return Math.hypot(second[0] - first[0], second[1] - first[1]);
}
function number(value) { return Number(value).toFixed(3).replace(/\.000$/, ""); }
function worldToScreen(view, p) { const s = baseScale * view.zoom; return { x: view.ox + p[0] * s, y: view.oy - p[1] * s }; }
function screenToWorld(view, x, y) { const s = baseScale * view.zoom; return { x: (x - view.ox) / s, y: (view.oy - y) / s }; }
function localPoint(canvas, event) { const r = canvas.getBoundingClientRect(); return { x: event.clientX - r.left, y: event.clientY - r.top }; }
function clamp(value, min, max) { return Math.max(min, Math.min(max, value)); }
function niceStep(target) { const p = 10 ** Math.floor(Math.log10(target)); const n = target / p; return (n < 2 ? 1 : n < 5 ? 2 : 5) * p; }
function line(ctx, x1, y1, x2, y2) { ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke(); }
function strokeEdgePath(view, ctx, edges) {
    if (!edges.length) return;
    ctx.beginPath();
    let end = null;
    for (const edge of edges) {
        let [startId, endId] = edge;
        if (end === endId) [startId, endId] = [endId, startId];
        const start = worldToScreen(view, view.vertices[startId]);
        const next = worldToScreen(view, view.vertices[endId]);
        if (end !== startId) ctx.moveTo(start.x, start.y);
        ctx.lineTo(next.x, next.y);
        end = endId;
    }
    ctx.stroke();
}
function edgeId(a, b) { return a < b ? `${a}-${b}` : `${b}-${a}`; }
function selectionKey(selection) { return `${selection.type}:${selection.id}`; }
function selectionKeyOrEmpty(selection) { return selection ? selectionKey(selection) : ""; }
function addSelection(view, selection) { view.selections.set(selectionKey(selection), selection); }
function capitalize(value) { return value.charAt(0).toUpperCase() + value.slice(1); }
