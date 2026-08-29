window.meshViewer = {
    canvas: null,
    context: null,
    points: [],
    lines: [],
    surfaces: [],
    meshNodes: [],
    yaw: -.65,
    pitch: .65,
    scale: 80,
    panX: 0,
    panY: 0,
    center: { x: 0, y: 0, z: 0 },
    pointers: new Map(),
    gesture: null,

    initialize(canvasId) {
        if (this.canvas) return;
        this.canvas = document.getElementById(canvasId);
        if (!this.canvas) return;
        this.context = this.canvas.getContext("2d");

        new ResizeObserver(() => this.resize()).observe(this.canvas);
        this.canvas.addEventListener("contextmenu", event => event.preventDefault());
        this.canvas.addEventListener("wheel", event => {
            event.preventDefault();
            const factor = Math.exp(-event.deltaY * .0012);
            this.zoomAt(factor, event.offsetX, event.offsetY);
        }, { passive: false });
        this.canvas.addEventListener("pointerdown", event => this.pointerDown(event));
        this.canvas.addEventListener("pointermove", event => this.pointerMove(event));
        this.canvas.addEventListener("pointerup", event => this.pointerUp(event));
        this.canvas.addEventListener("pointercancel", event => this.pointerUp(event));
        this.canvas.addEventListener("keydown", event => this.keyDown(event));
        this.resize();
    },

    setScene(points, lines, surfaces, meshNodes) {
        this.points = points ?? [];
        this.lines = lines ?? [];
        this.surfaces = surfaces ?? [];
        this.meshNodes = meshNodes ?? [];
        this.fit();
    },

    reset() {
        this.yaw = -.65;
        this.pitch = .65;
        this.panX = 0;
        this.panY = 0;
        this.fit();
    },

    fit() {
        if (!this.canvas) return;
        if (this.points.length === 0) {
            this.center = { x: 0, y: 0, z: 0 };
            this.scale = 80;
            this.panX = 0;
            this.panY = 0;
            this.draw();
            return;
        }

        const xs = this.points.map(point => point.x);
        const ys = this.points.map(point => point.y);
        const zs = this.points.map(point => point.z);
        const min = { x: Math.min(...xs), y: Math.min(...ys), z: Math.min(...zs) };
        const max = { x: Math.max(...xs), y: Math.max(...ys), z: Math.max(...zs) };
        this.center = {
            x: (min.x + max.x) / 2,
            y: (min.y + max.y) / 2,
            z: (min.z + max.z) / 2
        };
        const span = Math.max(max.x - min.x, max.y - min.y, max.z - min.z, .25);
        this.scale = Math.max(10, Math.min(this.canvas.clientWidth, this.canvas.clientHeight) * .68 / span);
        this.panX = 0;
        this.panY = 0;
        this.draw();
    },

    resize() {
        if (!this.canvas) return;
        const ratio = Math.min(window.devicePixelRatio || 1, 2);
        const width = Math.max(1, this.canvas.clientWidth);
        const height = Math.max(1, this.canvas.clientHeight);
        this.canvas.width = Math.round(width * ratio);
        this.canvas.height = Math.round(height * ratio);
        this.context.setTransform(ratio, 0, 0, ratio, 0, 0);
        this.draw();
    },

    pointerDown(event) {
        this.canvas.focus({ preventScroll: true });
        this.canvas.setPointerCapture(event.pointerId);
        this.pointers.set(event.pointerId, { x: event.offsetX, y: event.offsetY, button: event.button, shift: event.shiftKey });
        this.gesture = this.gestureState();
    },

    pointerMove(event) {
        if (!this.pointers.has(event.pointerId)) return;
        const previous = this.gesture;
        const original = this.pointers.get(event.pointerId);
        this.pointers.set(event.pointerId, {
            x: event.offsetX,
            y: event.offsetY,
            button: original.button,
            shift: original.shift || event.shiftKey
        });
        const current = this.gestureState();
        if (!previous) {
            this.gesture = current;
            return;
        }

        if (this.pointers.size > 1) {
            this.panX += current.x - previous.x;
            this.panY += current.y - previous.y;
            if (previous.distance > 0) this.scale *= current.distance / previous.distance;
        } else {
            const pointer = [...this.pointers.values()][0];
            const dx = current.x - previous.x;
            const dy = current.y - previous.y;
            if (pointer.shift || pointer.button === 1 || pointer.button === 2) {
                this.panX += dx;
                this.panY += dy;
            } else {
                this.yaw += dx * .009;
                this.pitch = Math.max(-1.5, Math.min(1.5, this.pitch + dy * .009));
            }
        }

        this.gesture = current;
        this.draw();
    },

    pointerUp(event) {
        this.pointers.delete(event.pointerId);
        this.gesture = this.gestureState();
    },

    gestureState() {
        const values = [...this.pointers.values()];
        if (values.length === 0) return null;
        if (values.length === 1) return { x: values[0].x, y: values[0].y, distance: 0 };
        const dx = values[1].x - values[0].x;
        const dy = values[1].y - values[0].y;
        return {
            x: (values[0].x + values[1].x) / 2,
            y: (values[0].y + values[1].y) / 2,
            distance: Math.hypot(dx, dy)
        };
    },

    keyDown(event) {
        const handled = ["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "+", "=", "-", "0"].includes(event.key);
        if (!handled) return;
        event.preventDefault();
        if (event.key === "0") return this.reset();
        if (event.key === "+" || event.key === "=") this.scale *= 1.12;
        else if (event.key === "-") this.scale /= 1.12;
        else if (event.shiftKey) {
            this.panX += event.key === "ArrowLeft" ? -16 : event.key === "ArrowRight" ? 16 : 0;
            this.panY += event.key === "ArrowUp" ? -16 : event.key === "ArrowDown" ? 16 : 0;
        } else {
            this.yaw += event.key === "ArrowLeft" ? -.08 : event.key === "ArrowRight" ? .08 : 0;
            this.pitch += event.key === "ArrowUp" ? -.08 : event.key === "ArrowDown" ? .08 : 0;
        }
        this.draw();
    },

    zoomAt(factor, x, y) {
        const oldScale = this.scale;
        this.scale = Math.max(2, Math.min(100000, this.scale * factor));
        const actual = this.scale / oldScale;
        this.panX = x - this.canvas.clientWidth / 2 - (x - this.canvas.clientWidth / 2 - this.panX) * actual;
        this.panY = y - this.canvas.clientHeight / 2 - (y - this.canvas.clientHeight / 2 - this.panY) * actual;
        this.draw();
    },

    project(point) {
        const x = point.x - this.center.x;
        const y = point.y - this.center.y;
        const z = point.z - this.center.z;
        const cy = Math.cos(this.yaw);
        const sy = Math.sin(this.yaw);
        const cp = Math.cos(this.pitch);
        const sp = Math.sin(this.pitch);
        const rotatedX = cy * x - sy * y;
        const rotatedY = sy * x + cy * y;
        const screenY = cp * z - sp * rotatedY;
        return {
            x: this.canvas.clientWidth / 2 + rotatedX * this.scale + this.panX,
            y: this.canvas.clientHeight / 2 - screenY * this.scale + this.panY,
            depth: sp * z + cp * rotatedY
        };
    },

    draw() {
        if (!this.context || !this.canvas) return;
        const ctx = this.context;
        const width = this.canvas.clientWidth;
        const height = this.canvas.clientHeight;
        ctx.clearRect(0, 0, width, height);
        ctx.fillStyle = "#080f1b";
        ctx.fillRect(0, 0, width, height);
        this.drawGrid(ctx);

        if (this.points.length === 0) {
            ctx.fillStyle = "#64748b";
            ctx.font = "12px system-ui";
            ctx.textAlign = "center";
            ctx.fillText("Run a script with Point and Line primitives", width / 2, height / 2);
            return;
        }

        const byTag = new Map(this.points.map(point => [point.tag, point]));
        const linesByTag = new Map(this.lines.map(line => [line.tag, line]));
        this.drawSurfaces(ctx, byTag, linesByTag);

        ctx.lineCap = "round";
        ctx.lineWidth = 2.6;
        ctx.strokeStyle = "#7dd3fc";
        ctx.shadowColor = "rgba(14, 165, 233, .55)";
        ctx.shadowBlur = 4;
        for (const line of this.lines) {
            const path = (line.path ?? []).map(point => this.project(point));
            if (path.length < 2) continue;
            ctx.beginPath();
            ctx.moveTo(path[0].x, path[0].y);
            for (let index = 1; index < path.length; index++) {
                ctx.lineTo(path[index].x, path[index].y);
            }
            ctx.stroke();
        }
        ctx.shadowColor = "transparent";
        ctx.shadowBlur = 0;

        const projectedMeshNodes = this.meshNodes
            .map(node => this.project(node))
            .sort((a, b) => a.depth - b.depth);
        for (const node of projectedMeshNodes) {
            ctx.beginPath();
            ctx.arc(node.x, node.y, 2.6, 0, Math.PI * 2);
            ctx.fillStyle = "#fbbf24";
            ctx.fill();
            ctx.strokeStyle = "#fff7d6";
            ctx.lineWidth = 1;
            ctx.stroke();
        }

        const projected = this.points
            .map(point => ({ point, screen: this.project(point) }))
            .sort((a, b) => a.screen.depth - b.screen.depth);
        for (const item of projected) {
            ctx.beginPath();
            ctx.arc(item.screen.x, item.screen.y, 5.5, 0, Math.PI * 2);
            ctx.fillStyle = "#22c55e";
            ctx.fill();
            ctx.strokeStyle = "#f0fdf4";
            ctx.lineWidth = 2;
            ctx.stroke();
            ctx.fillStyle = "#f8fafc";
            ctx.font = "600 11px ui-monospace, monospace";
            ctx.textAlign = "left";
            ctx.fillText(String(item.point.tag), item.screen.x + 7, item.screen.y - 6);
        }
    },

    drawSurfaces(ctx, pointsByTag, linesByTag) {
        const projectedSurfaces = this.surfaces
            .map(surface => ({
                surface,
                loops: (surface.loops ?? [])
                    .map(loop => this.surfaceLoopPoints(loop, linesByTag))
                    .filter(points => points.length >= 3)
            }))
            .filter(item => item.loops.length > 0)
            .map(item => ({
                ...item,
                projected: item.loops.map(loop => loop.map(point => this.project(point)))
            }))
            .sort((a, b) => {
                const depth = item => item.projected.flat().reduce((sum, point) => sum + point.depth, 0) /
                    item.projected.flat().length;
                return depth(a) - depth(b);
            });

        for (const item of projectedSurfaces) {
            ctx.beginPath();
            for (const loop of item.projected) {
                ctx.moveTo(loop[0].x, loop[0].y);
                for (let index = 1; index < loop.length; index++) {
                    ctx.lineTo(loop[index].x, loop[index].y);
                }
                ctx.closePath();
            }
            const hue = 205 + (item.surface.tag * 29) % 55;
            ctx.fillStyle = `hsla(${hue}, 82%, 58%, .24)`;
            ctx.fill("evenodd");
        }
    },

    surfaceLoopPoints(orientedTags, linesByTag) {
        const points = [];
        for (const orientedTag of orientedTags ?? []) {
            const curve = linesByTag.get(Math.abs(orientedTag));
            if (!curve?.path?.length) return [];
            const path = orientedTag < 0 ? [...curve.path].reverse() : curve.path;
            points.push(...(points.length === 0 ? path : path.slice(1)));
        }
        return points;
    },

    drawGrid(ctx) {
        const extent = this.points.length
            ? Math.max(...this.points.flatMap(point => [Math.abs(point.x - this.center.x), Math.abs(point.y - this.center.y)]), 1)
            : 2;
        const step = Math.pow(10, Math.floor(Math.log10(extent))) / 2;
        const size = Math.ceil(extent * 2 / step) * step;
        ctx.lineWidth = 1;
        for (let value = -size; value <= size + step * .5; value += step) {
            const xStart = this.project({ x: this.center.x + value, y: this.center.y - size, z: 0 });
            const xEnd = this.project({ x: this.center.x + value, y: this.center.y + size, z: 0 });
            const yStart = this.project({ x: this.center.x - size, y: this.center.y + value, z: 0 });
            const yEnd = this.project({ x: this.center.x + size, y: this.center.y + value, z: 0 });
            ctx.strokeStyle = Math.abs(value) < step * .1 ? "#475569" : "#1c293b";
            ctx.beginPath(); ctx.moveTo(xStart.x, xStart.y); ctx.lineTo(xEnd.x, xEnd.y); ctx.stroke();
            ctx.beginPath(); ctx.moveTo(yStart.x, yStart.y); ctx.lineTo(yEnd.x, yEnd.y); ctx.stroke();
        }
    }
};
