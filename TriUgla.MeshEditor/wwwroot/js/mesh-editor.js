const views = new WeakMap();
const baseScale = 48;

export function initialize(canvas, dotnet, mesh) {
    const view = {
        canvas, dotnet, zoom: 1, ox: 0, oy: 0, dragging: false, pointerId: null, x: 0, y: 0,
        moved: false, selections: new Map(), hover: null, message: null, showSuperStructure: true,
        canUndo: false, canRedo: false, tool: "select", constraintStart: null,
        readOnly: false, viewMode: "edit", quadVertices: [], quads: [], quadTriangles: [], quadEdgeFlips: 0,
        yaw: -.65, pitch: .72,
        brushRadius: .75, brushStroke: [],
        polylineNodes: [], polygonNodes: [],
        mutationQueue: Promise.resolve(),
        meshVersion: 0,
        renderPending: false, reportTimer: null, hoverLookupTimer: null, hoverPoint: null,
        hoverLookupVersion: 0,
        hoverLine: [], hoverRequest: 0,
        vertices: [], nodeKinds: [], nodeConstraintCounts: [], nodeElevations: [], nodeTargetAreas: [], triangles: [], boundaryEdges: [], loopEdges: [], constraintEdges: [],
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
    view.doubleClick = event => onDoubleClick(view, event);
    view.contextMenu = event => deleteNode(view, event);
    canvas.addEventListener("wheel", view.wheel, { passive: false });
    canvas.addEventListener("pointerdown", view.down);
    canvas.addEventListener("pointermove", view.move);
    canvas.addEventListener("pointerup", view.up);
    canvas.addEventListener("pointercancel", view.up);
    canvas.addEventListener("pointerleave", view.leave);
    canvas.addEventListener("keydown", view.key);
    canvas.addEventListener("dblclick", view.doubleClick);
    canvas.addEventListener("contextmenu", view.contextMenu);
    sizeCanvas(view);
    fit(canvas);
}

export function reset(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    if (view.viewMode === "3d") {
        view.yaw = -.65;
        view.pitch = .72;
        fit3D(view);
        changed(view);
        return;
    }
    view.zoom = 1;
    view.ox = view.width / 2;
    view.oy = view.height / 2;
    changed(view);
}

export function fit(canvas) {
    const view = views.get(canvas);
    if (!view || !view.width || !view.height) return;
    if (view.viewMode === "3d") {
        fit3D(view);
        changed(view);
        return;
    }
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
    return enqueueMutation(view, async () => {
        applyMesh(view, await view.dotnet.invokeMethodAsync("ResetMeshState"));
        view.message = null;
        fit(canvas);
    });
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

export function setBrushRadius(canvas, radius) {
    const view = views.get(canvas);
    if (!view || !Number.isFinite(radius) || radius <= 0) return;
    view.brushRadius = radius;
    requestDraw(view);
}

export function showQuadMesh(canvas, overlay) {
    const view = views.get(canvas);
    if (!view) return;
    view.readOnly = true;
    view.viewMode = "quad";
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
    view.viewMode = "edit";
    view.quadVertices = [];
    view.quads = [];
    view.quadTriangles = [];
    view.quadEdgeFlips = 0;
    view.message = null;
    changed(view);
}

export function show3DView(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    view.readOnly = true;
    view.viewMode = "3d";
    view.tool = "select";
    view.selections.clear();
    view.hover = null;
    view.message = "Elevation is shown as Z";
    fit3D(view);
    changed(view);
}

export function hide3DView(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    view.readOnly = false;
    view.viewMode = "edit";
    view.message = null;
    fit(canvas);
}

export async function undo(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    return enqueueMutation(view, async () => {
        if (view.readOnly || !view.canUndo) return;
        const mesh = await view.dotnet.invokeMethodAsync("UndoAction");
        applyMesh(view, mesh);
        view.message = mesh.succeeded ? null : mesh.failureReason ?? "Undo failed";
        changed(view);
    });
}

export async function redo(canvas) {
    const view = views.get(canvas);
    if (!view) return;
    return enqueueMutation(view, async () => {
        if (view.readOnly || !view.canRedo) return;
        const mesh = await view.dotnet.invokeMethodAsync("RedoAction");
        applyMesh(view, mesh);
        view.message = mesh.succeeded ? null : mesh.failureReason ?? "Redo failed";
        changed(view);
    });
}

export async function updateNodeData(canvas, nodeId, elevation, targetArea) {
    const view = views.get(canvas);
    if (!view || view.readOnly) return;
    return enqueueMutation(view, async () => {
        const mesh = await view.dotnet.invokeMethodAsync(
            "UpdateNodeData", nodeId, elevation, targetArea);
        applyMesh(view, mesh);
        if (mesh.succeeded) addSelection(view, { type: "node", id: nodeId });
        view.message = mesh.succeeded ? null : mesh.failureReason ?? "Node properties could not be updated";
        changed(view);
    });
}

export async function updateSelectedNodeProperty(canvas, nodeIds, propertyName, value) {
    const view = views.get(canvas);
    if (!view || view.readOnly || !nodeIds.length) return;
    return enqueueMutation(view, async () => {
        const mesh = await view.dotnet.invokeMethodAsync(
            "UpdateNodeProperty", nodeIds, propertyName, value);
        applyMesh(view, mesh);
        if (mesh.succeeded) {
            for (const id of nodeIds) {
                if (view.vertices[id]) addSelection(view, { type: "node", id });
            }
        }
        view.message = mesh.succeeded ? null : mesh.failureReason ?? "Node properties could not be updated";
        changed(view);
    });
}

export async function refineSelectedFaces(canvas, refineLand, refineLakes) {
    const view = views.get(canvas);
    if (!view || view.readOnly) return;
    const faceIds = [...view.selections.values()]
        .filter(item => item.type === "face")
        .map(item => item.id);
    return enqueueMutation(view, async () => {
        const mesh = faceIds.length
            ? await view.dotnet.invokeMethodAsync("RefineFaces", faceIds, refineLand, refineLakes)
            : await view.dotnet.invokeMethodAsync("RefineAll", refineLand, refineLakes);
        applyMesh(view, mesh);
        view.message = mesh.succeeded ? null : mesh.failureReason ?? "Mesh could not be refined";
        changed(view);
    });
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
    canvas.removeEventListener("dblclick", view.doubleClick);
    canvas.removeEventListener("contextmenu", view.contextMenu);
    if (view.reportTimer !== null) clearTimeout(view.reportTimer);
    if (view.hoverLookupTimer !== null) clearTimeout(view.hoverLookupTimer);
    views.delete(canvas);
}

function onWheel(view, event) {
    event.preventDefault();
    if (view.viewMode === "3d") {
        view.zoom = clamp(view.zoom * Math.exp(-event.deltaY * .0015), .05, 100);
        changed(view);
        return;
    }
    const p = localPoint(view.canvas, event);
    zoomAround(view, p.x, p.y, Math.exp(-event.deltaY * .0015));
}

function onPointerDown(view, event) {
    if (event.button !== 0 && event.button !== 1) return;
    event.preventDefault();
    view.canvas.focus();
    const brushing = !view.readOnly && event.button === 0 &&
        (view.tool === "density-increase" || view.tool === "density-decrease");
    view.rotating = view.viewMode === "3d" && event.button === 0;
    view.dragging = event.button === 1;
    view.selecting = event.button === 0 && !brushing;
    view.brushing = brushing;
    view.pointerId = event.pointerId;
    view.lastX = event.clientX;
    view.lastY = event.clientY;
    view.downX = event.clientX;
    view.downY = event.clientY;
    view.moved = false;
    view.canvas.setPointerCapture(event.pointerId);
    if (brushing) {
        const p = localPoint(view.canvas, event);
        const world = screenToWorld(view, p.x, p.y);
        view.x = world.x; view.y = world.y;
        view.brushStroke = [[world.x, world.y]];
        view.suppressBrush = true;
        draw(view);
        view.suppressBrush = false;
        view.brushBase = document.createElement("canvas");
        view.brushBase.width = view.canvas.width;
        view.brushBase.height = view.canvas.height;
        view.brushBase.getContext("2d").drawImage(view.canvas, 0, 0);
        view.canvas.classList.add("brushing");
        requestBrushDraw(view);
    }
    if (view.dragging || view.rotating) view.canvas.classList.add("dragging");
}

function onPointerMove(view, event) {
    const p = localPoint(view.canvas, event);
    const world = screenToWorld(view, p.x, p.y);
    view.x = world.x;
    view.y = world.y;
    if (event.pointerId === view.pointerId &&
        (event.clientX - view.downX) ** 2 + (event.clientY - view.downY) ** 2 > 16) view.moved = true;
    if (view.rotating && event.pointerId === view.pointerId) {
        view.yaw += (event.clientX - view.lastX) * .008;
        view.pitch = clamp(view.pitch + (event.clientY - view.lastY) * .008, -.05, Math.PI / 2 - .05);
        view.lastX = event.clientX;
        view.lastY = event.clientY;
        draw(view);
    } else if (view.dragging && event.pointerId === view.pointerId) {
        view.hoverLookupVersion++;
        view.hoverPoint = null;
        setHover(view, null);
        view.ox += event.clientX - view.lastX;
        view.oy += event.clientY - view.lastY;
        view.lastX = event.clientX;
        view.lastY = event.clientY;
        draw(view);
    } else if (view.brushing && event.pointerId === view.pointerId) {
        const previous = view.brushStroke[view.brushStroke.length - 1];
        const spacing = Math.max(view.brushRadius * .5, 4 / (baseScale * view.zoom));
        if (!previous || (world.x - previous[0]) ** 2 + (world.y - previous[1]) ** 2 >= spacing ** 2)
            view.brushStroke.push([world.x, world.y]);
        requestBrushDraw(view);
    } else {
        scheduleMeshHover(view, p, event.ctrlKey);
    }
    if (view.polylineNodes.length || view.polygonNodes.length) requestDraw(view);
    scheduleReport(view);
}

function onPointerUp(view, event) {
    if (event.pointerId !== view.pointerId) return;
    const shouldSelect = view.selecting && !view.moved && view.viewMode !== "3d";
    const brushStroke = view.brushing && event.type !== "pointercancel" ? view.brushStroke.slice() : null;
    const increaseDensity = view.tool === "density-increase";
    view.dragging = false;
    view.rotating = false;
    view.selecting = false;
    view.brushing = false;
    view.pointerId = null;
    view.canvas.classList.remove("dragging");
    view.canvas.classList.remove("brushing");
    view.brushStroke = [];
    view.brushBase = null;
    if (brushStroke?.length) {
        applyDensityBrush(view, brushStroke, increaseDensity);
        return;
    }
    if (shouldSelect && !view.readOnly && view.tool === "constraint") chooseConstraintNode(view, event);
    else if (shouldSelect && view.tool === "polyline") choosePolylineNode(view, event);
    else if (shouldSelect && view.tool === "polygon") choosePolygonNode(view, event);
    else if (shouldSelect && event.metaKey && !view.readOnly) insertNode(view, event);
    else if (shouldSelect) selectAt(view, localPoint(view.canvas, event), event);
}

async function applyDensityBrush(view, stroke, increaseDensity) {
    return enqueueMutation(view, async () => {
        const coordinates = stroke.flat();
        const mesh = await view.dotnet.invokeMethodAsync(
            "BrushDensity", coordinates, increaseDensity);
        applyMesh(view, mesh);
        view.message = mesh.succeeded ? "Brush applied" :
            mesh.failureReason ?? "Brush failed";
        changed(view);
    });
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
    return enqueueMutation(view, async () => {
        if (view.readOnly) return;
        const selected = [...view.selections.values()];
        const constraintIds = [...new Set(selected
            .filter(item => item.type === "constraint").map(item => item.id))];
        const nodeIds = [...new Set(selected
            .filter(item => item.type === "node").map(item => item.id))];
        const mesh = constraintIds.length || nodeIds.length
            ? await view.dotnet.invokeMethodAsync("RemoveElements", constraintIds, nodeIds)
            : null;
        const changedMesh = mesh?.succeeded === true;
        if (mesh) applyMesh(view, mesh);
        view.message = changedMesh ? null : mesh?.failureReason ?? "Selected elements cannot be deleted";
        changed(view);
    });
}

function enqueueMutation(view, mutation) {
    const queued = view.mutationQueue.then(mutation, mutation);
    view.mutationQueue = queued.catch(() => {});
    return queued;
}

async function insertNode(view, event) {
    return enqueueMutation(view, async () => {
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
        applyMesh(view, mesh);
        if (!mesh.succeeded || mesh.changedNodeId === null) {
            view.message = mesh.failureReason ?? "Node could not be inserted";
            changed(view);
            return;
        }
        addSelection(view, { type: "node", id: mesh.changedNodeId });
        view.message = null;
        changed(view);
    });
}

async function deleteNode(view, event) {
    event.preventDefault();
    if (event.ctrlKey) {
        if (Date.now() - (view.lastUnderlyingSelection ?? 0) < 250) return;
        return selectAt(view, localPoint(view.canvas, event), event);
    }
    return enqueueMutation(view, async () => {
        if (view.readOnly) return;
        const screen = localPoint(view.canvas, event);
        const hit = await resolveHit(view, screen);
        if (hit?.type !== "node" && hit?.type !== "constraint") return;
        const mesh = hit.type === "constraint"
            ? await view.dotnet.invokeMethodAsync("RemoveConstraintLine", hit.id)
            : await view.dotnet.invokeMethodAsync("RemoveNode", hit.id);
        applyMesh(view, mesh);
        if (!mesh.succeeded) {
            view.message = hit.type === "constraint"
                ? mesh.failureReason ?? "Constraint cannot be removed"
                : mesh.failureReason ?? `Node ${hit.id} cannot be removed`;
            report(view);
            return;
        }
        view.message = null;
        changed(view);
    });
}

function applyMesh(view, mesh) {
    view.meshVersion++;
    view.hoverLookupVersion++;
    view.hoverRequest++;
    view.hoverPoint = null;
    view.hover = null;
    view.hoverLine = [];
    view.selections.clear();
    const maxId = mesh.nodes.reduce((maximum, node) => Math.max(maximum, node.id), -1);
    view.vertices = Array(maxId + 1).fill(null);
    view.nodeKinds = Array(maxId + 1).fill("Normal");
    view.nodeConstraintCounts = Array(maxId + 1).fill(0);
    view.nodeElevations = Array(maxId + 1).fill(0);
    view.nodeTargetAreas = Array(maxId + 1).fill(0);
    for (const node of mesh.nodes) {
        view.vertices[node.id] = [node.x, node.y];
        view.nodeKinds[node.id] = node.kind;
        view.nodeConstraintCounts[node.id] = node.constraintCount;
        view.nodeElevations[node.id] = node.elevation;
        view.nodeTargetAreas[node.id] = node.targetArea;
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
    const hit = await resolveHit(view, screen, event.ctrlKey);
    if (event.ctrlKey) view.lastUnderlyingSelection = Date.now();
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

async function onDoubleClick(view, event) {
    if (view.tool !== "select") return;
    event.preventDefault();
    const hit = await resolveHit(view, localPoint(view.canvas, event), event.ctrlKey);
    if (!hit) return;
    const kind = selectionKind(view, hit);
    const matches = selectionsOfKind(view, hit.type, kind);
    if (!event.shiftKey) view.selections.clear();
    for (const match of matches) addSelection(view, match);
    view.message = `${matches.length} ${kind} ${hit.type}${matches.length === 1 ? "" : "s"} selected`;
    changed(view);
}

function selectionsOfKind(view, type, kind) {
    if (type === "node") {
        return view.vertices.flatMap((vertex, id) =>
            vertex && (view.showSuperStructure || !view.superNodes.has(id)) && view.nodeKinds[id] === kind
                ? [{ type: "node", id, constraintCount: view.nodeConstraintCounts[id] }]
                : []);
    }
    if (type === "face") {
        return view.triangles.flatMap((triangle, id) =>
            (view.showSuperStructure || !view.superFaces.has(id)) && view.faceKinds[id] === kind
                ? [{ type: "face", id }]
                : []);
    }
    if (type === "constraint") {
        return kind !== "Feature" ? [] : view.constraints.map(constraint => ({
            type: "constraint", id: constraint.id, constraint,
            a: constraint.startNodeId, b: constraint.endNodeId,
            edges: constraint.edges.map(edge => [edge.a, edge.b])
        }));
    }

    const seen = new Set();
    const matches = [];
    for (const triangle of view.triangles) {
        for (const [a, b] of [[triangle[0], triangle[1]], [triangle[1], triangle[2]], [triangle[2], triangle[0]]]) {
            const id = edgeId(a, b);
            if (seen.has(id)) continue;
            seen.add(id);
            if (!view.showSuperStructure && (view.superNodes.has(a) || view.superNodes.has(b))) continue;
            const candidate = { type: "edge", id, a, b, constraintCount: 0 };
            if (selectionKind(view, candidate) === kind) matches.push(candidate);
        }
    }
    return matches;
}

function selectionKind(view, selection) {
    if (selection.type === "node") return view.nodeKinds[selection.id];
    if (selection.type === "face") return view.faceKinds[selection.id];
    if (selection.type === "constraint") return "Feature";
    return edgeKind(view, selection);
}

function edgeKind(view, edge) {
    return view.loopEdges.some(candidate => edgeId(candidate[0], candidate[1]) === edge.id)
        ? "Boundary" : view.constraintEdgeIds.has(edge.id) ? "Feature" : "Interior";
}

function setHover(view, hit) {
    if (selectionKeyOrEmpty(view.hover) === selectionKeyOrEmpty(hit)) return;
    view.hover = hit;
    view.hoverLine = [];
    view.hoverRequest++;
    if (hit?.type === "constraint") view.hoverLine = hit.edges ?? [];
    else if (hit?.type === "edge") view.hoverLine = [[hit.a, hit.b]];
    draw(view);
}

async function chooseConstraintNode(view, event) {
    return enqueueMutation(view, () => chooseConstraintNodeCore(view, event));
}

async function chooseConstraintNodeCore(view, event) {
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
    applyMesh(view, mesh);
    if (!mesh.succeeded) {
        view.message = mesh.failureReason ?? "Constraint could not be inserted";
        report(view);
        return;
    }
    view.constraintStart = null;
    view.message = null;
    changed(view);
}

async function choosePolylineNode(view, event) {
    return enqueueMutation(view, () => choosePolylineNodeCore(view, event));
}

async function choosePolylineNodeCore(view, event) {
    const node = await nodeAtOrInsert(view, event);
    if (!node || view.superNodes.has(node.id) || view.polylineNodes.at(-1) === node.id) return;
    view.polylineNodes.push(node.id);
    addSelection(view, node);
    view.message = `${view.polylineNodes.length} polyline points — Enter to finish`;
    changed(view);
}

async function finishPolyline(view) {
    return enqueueMutation(view, () => finishPolylineCore(view));
}

async function finishPolylineCore(view) {
    if (view.polylineNodes.length < 2) return;
    const mesh = await view.dotnet.invokeMethodAsync("InsertPolyline", view.polylineNodes);
    applyMesh(view, mesh);
    if (!mesh.succeeded) {
        view.message = mesh.failureReason ?? "Polyline could not be inserted";
        report(view);
        return;
    }
    view.polylineNodes = [];
    view.message = null;
    changed(view);
}

async function choosePolygonNode(view, event) {
    return enqueueMutation(view, () => choosePolygonNodeCore(view, event));
}

async function choosePolygonNodeCore(view, event) {
    const node = await nodeAtOrInsert(view, event);
    if (!node || view.superNodes.has(node.id) || view.polygonNodes.includes(node.id)) return;
    view.polygonNodes.push(node.id);
    addSelection(view, node);
    view.message = `${view.polygonNodes.length} polygon vertices — Enter to close`;
    changed(view);
}

async function finishPolygon(view) {
    return enqueueMutation(view, () => finishPolygonCore(view));
}

async function finishPolygonCore(view) {
    if (view.polygonNodes.length < 3) return;
    const mesh = await view.dotnet.invokeMethodAsync("InsertPolygon", view.polygonNodes);
    applyMesh(view, mesh);
    if (!mesh.succeeded) {
        view.message = mesh.failureReason ?? "Polygon could not be inserted";
        report(view);
        return;
    }
    view.polygonNodes = [];
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
    applyMesh(view, inserted);
    if (!inserted.succeeded || inserted.changedNodeId === null) return null;
    return { type: "node", id: inserted.changedNodeId };
}

function syncDraftSelection(view, ids) {
    view.selections.clear();
    for (const id of ids) addSelection(view, { type: "node", id });
}

function scheduleMeshHover(view, screen, underlying = false) {
    view.hoverPoint = screen;
    view.hoverUnderlying = underlying;
    view.hoverLookupVersion++;
    if (view.hoverLookupTimer !== null) return;
    view.hoverLookupTimer = setTimeout(async () => {
        view.hoverLookupTimer = null;
        const point = view.hoverPoint;
        const preferUnderlying = view.hoverUnderlying;
        view.hoverPoint = null;
        const version = view.hoverLookupVersion;
        const hit = await resolveHit(view, point, preferUnderlying);
        if (version === view.hoverLookupVersion) setHover(view, hit);
    }, 32);
}

async function resolveHit(view, screen, underlying = false) {
    if (!screen) return null;
    const meshVersion = view.meshVersion;
    const world = screenToWorld(view, screen.x, screen.y);
    const hit = await view.dotnet.invokeMethodAsync(
        "FindElement", world.x, world.y, 9 / (baseScale * view.zoom));
    if (meshVersion !== view.meshVersion) return null;
    if (!hit) return null;
    if (hit.type === "node") {
        if (!view.showSuperStructure && view.superNodes.has(hit.id)) return null;
        return { type: "node", id: hit.id, constraintCount: hit.constraintCount };
    }
    if (hit.type === "face") {
        if (!view.showSuperStructure && view.superFaces.has(hit.id)) return null;
        return { type: "face", id: hit.id };
    }
    const a = hit.a, b = hit.b;
    if (!view.showSuperStructure && (view.superNodes.has(a) || view.superNodes.has(b))) return null;
    const id = edgeId(a, b);
    const constraint = view.constraintByEdge.get(id);
    return constraint && !underlying
        ? { type: "constraint", id: constraint.id, a, b, constraint,
            edges: constraint.edges.map(edge => [edge.a, edge.b]) }
        : { type: "edge", id, a, b, constraintCount: hit.constraintCount };
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
    if (view.viewMode === "3d") {
        draw3DMesh(view, ctx);
        return;
    }
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

function fit3D(view) {
    const ids = view.vertices
        .map((vertex, id) => vertex && (view.showSuperStructure || !view.superNodes.has(id)) ? id : -1)
        .filter(id => id >= 0);
    if (!ids.length || !view.width || !view.height) return;
    const xs = ids.map(id => view.vertices[id][0]);
    const ys = ids.map(id => view.vertices[id][1]);
    const zs = ids.map(id => view.nodeElevations[id] ?? 0);
    view.threeDCenter = {
        x: (Math.min(...xs) + Math.max(...xs)) / 2,
        y: (Math.min(...ys) + Math.max(...ys)) / 2,
        z: (Math.min(...zs) + Math.max(...zs)) / 2
    };
    const raw = ids.map(id => project3DRaw(view, id));
    const minX = Math.min(...raw.map(p => p.x)), maxX = Math.max(...raw.map(p => p.x));
    const minY = Math.min(...raw.map(p => p.y)), maxY = Math.max(...raw.map(p => p.y));
    const padding = Math.min(80, Math.min(view.width, view.height) * .14);
    view.zoom = clamp(Math.min(
        (view.width - padding * 2) / (Math.max(maxX - minX, .01) * baseScale),
        (view.height - padding * 2) / (Math.max(maxY - minY, .01) * baseScale)), .05, 100);
    const scale = baseScale * view.zoom;
    view.ox = view.width / 2 - (minX + maxX) * scale / 2;
    view.oy = view.height / 2 + (minY + maxY) * scale / 2;
}

function project3DRaw(view, id) {
    const vertex = view.vertices[id];
    const center = view.threeDCenter ?? { x: 0, y: 0, z: 0 };
    const dx = vertex[0] - center.x;
    const dy = vertex[1] - center.y;
    const dz = (view.nodeElevations[id] ?? 0) - center.z;
    const cosY = Math.cos(view.yaw), sinY = Math.sin(view.yaw);
    const cosP = Math.cos(view.pitch), sinP = Math.sin(view.pitch);
    const rx = cosY * dx - sinY * dy;
    const ry = sinY * dx + cosY * dy;
    return { x: rx, y: cosP * ry - sinP * dz, depth: sinP * ry + cosP * dz };
}

function project3D(view, id) {
    const p = project3DRaw(view, id);
    const scale = baseScale * view.zoom;
    return { x: view.ox + p.x * scale, y: view.oy - p.y * scale, depth: p.depth };
}

function draw3DMesh(view, ctx) {
    const faces = view.triangles
        .map((triangle, index) => ({ triangle, index, points: triangle.map(id => project3D(view, id)) }))
        .filter(face => view.showSuperStructure || !view.superFaces.has(face.index))
        .sort((a, b) => a.points.reduce((sum, p) => sum + p.depth, 0) -
                        b.points.reduce((sum, p) => sum + p.depth, 0));

    ctx.save();
    ctx.lineJoin = "round";
    for (const face of faces) {
        const [a, b, c] = face.points;
        const kind = view.faceKinds[face.index];
        const shade = clamp(.42 + (a.depth + b.depth + c.depth) * .018, .26, .72);
        ctx.beginPath();
        ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.lineTo(c.x, c.y); ctx.closePath();
        ctx.fillStyle = kind === "Lake"
            ? `rgba(14, 165, 233, ${shade})`
            : kind === "Island"
                ? `rgba(34, 197, 94, ${shade})`
                : `rgba(100, 116, 139, ${shade * .65})`;
        ctx.strokeStyle = view.superFaces.has(face.index) ? "#6d5ba8" : "rgba(207, 250, 254, .72)";
        ctx.lineWidth = 1.1;
        ctx.fill();
        ctx.stroke();
    }
    for (const edges of [view.loopEdges, view.constraintEdges]) {
        ctx.strokeStyle = edges === view.loopEdges ? "#34d399" : "#f472b6";
        ctx.lineWidth = 2.2;
        for (const [start, end] of edges) {
            const a = project3D(view, start), b = project3D(view, end);
            line(ctx, a.x, a.y, b.x, b.y);
        }
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
    for (const [start, end] of view.constraintEdges) {
        const a = worldToScreen(view, view.vertices[start]);
        const b = worldToScreen(view, view.vertices[end]);
        line(ctx, a.x, a.y, b.x, b.y);
    }
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
    drawDensityBrush(view, ctx);
}

function drawDensityBrush(view, ctx) {
    if (view.suppressBrush ||
        view.tool !== "density-increase" && view.tool !== "density-decrease") return;
    const points = view.brushStroke.length ? view.brushStroke : [[view.x, view.y]];
    const radius = view.brushRadius * baseScale * view.zoom;
    ctx.save();
    ctx.fillStyle = view.tool === "density-increase" ? "rgba(34, 211, 238, .10)" : "rgba(245, 158, 11, .10)";
    ctx.strokeStyle = view.tool === "density-increase" ? "#22d3ee" : "#f59e0b";
    const screenPoints = points.map(point => worldToScreen(view, point));
    if (screenPoints.length > 1) {
        ctx.beginPath();
        ctx.moveTo(screenPoints[0].x, screenPoints[0].y);
        for (let index = 1; index < screenPoints.length; index++)
            ctx.lineTo(screenPoints[index].x, screenPoints[index].y);
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.lineWidth = radius * 2;
        ctx.strokeStyle = view.tool === "density-increase" ? "rgba(34, 211, 238, .16)" : "rgba(245, 158, 11, .16)";
        ctx.setLineDash([]);
        ctx.stroke();
    }
    const screen = screenPoints[screenPoints.length - 1];
    ctx.beginPath();
    ctx.arc(screen.x, screen.y, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = view.tool === "density-increase" ? "#22d3ee" : "#f59e0b";
    ctx.lineWidth = 1.5;
    ctx.setLineDash([6, 4]);
    ctx.stroke();
    ctx.restore();
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
function requestBrushDraw(view) {
    if (view.renderPending) return;
    view.renderPending = true;
    requestAnimationFrame(() => {
        view.renderPending = false;
        if (!view.brushBase) return draw(view);
        view.ctx.save();
        view.ctx.setTransform(1, 0, 0, 1, 0, 0);
        view.ctx.drawImage(view.brushBase, 0, 0);
        view.ctx.restore();
        drawDensityBrush(view, view.ctx);
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
    const selectedObjects = combineSelectionInfo(
        selected.map(item => describeSelection(view, item)));
    const selection = view.message || (selected.length === 0 ? "No selection" : selected.length === 1
        ? `${capitalize(selected[0].type)} ${selected[0].id}` : `${selected.length} selected`);
    const nodeCount = view.vertices.filter((vertex, id) => vertex && !view.superNodes.has(id)).length;
    const edges = new Set();
    for (const triangle of view.triangles) {
        for (const [a, b] of [[triangle[0], triangle[1]], [triangle[1], triangle[2]], [triangle[2], triangle[0]]]) {
            if (!view.superNodes.has(a) && !view.superNodes.has(b)) edges.add(edgeId(a, b));
        }
    }
    view.dotnet.invokeMethodAsync(
        "UpdateStatus", view.x, view.y, view.zoom,
        nodeCount, edges.size, view.constraints.length,
        selection, view.canUndo, view.canRedo, selectedObjects);
}

function combineSelectionInfo(items) {
    const groups = new Map();
    for (const item of items) {
        if (!groups.has(item.type)) groups.set(item.type, []);
        groups.get(item.type).push(item);
    }
    return [...groups.values()].map(group => {
        if (group.length === 1) return group[0];
        const properties = group[0].properties.map(property => {
            const values = group.map(item =>
                item.properties.find(candidate => candidate.name === property.name)?.value);
            const value = values.every(candidate => candidate === values[0])
                ? values[0]
                : "Varies";
            return { name: property.name, value };
        });
        return {
            id: null,
            ids: group.flatMap(item => item.ids),
            type: group[0].type,
            title: `${group.length} ${plural(group[0].type)}`,
            properties
        };
    });
}

function plural(value) {
    return value === "Face" ? "Faces" : `${value}s`;
}

function describeSelection(view, item) {
    if (item.type === "node") {
        const point = view.vertices[item.id];
        return selectionInfo("Node", `Node ${item.id}`, [
            ["ID", item.id], ["X", number(point[0])], ["Y", number(point[1])],
            ["Elevation", view.nodeElevations[item.id]],
            ["Target area", view.nodeTargetAreas[item.id]],
            ["Kind", view.nodeKinds[item.id]],
            ["ConstraintCount", view.nodeConstraintCounts[item.id]],
            ["Super structure", view.superNodes.has(item.id) ? "Yes" : "No"]
        ], item.id);
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
    const kind = edgeKind(view, item);
    return selectionInfo("Edge", `Edge ${item.id}`, [
        ["Start node", item.a], ["End node", item.b], ["Length", number(length)], ["Kind", kind],
        ["ConstraintCount", item.constraintCount ?? 0]
    ]);
}
function selectionInfo(type, title, entries, id = null) {
    return { id, ids: id === null ? [] : [id], type, title,
        properties: entries.map(entry => ({ name: entry[0], value: String(entry[1]) })) };
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
