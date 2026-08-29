window.editorInterop = {
    initialize(editorId, scriptObjects = {}) {
        const editor = document.getElementById(editorId);
        if (!editor || editor.dataset.initialized) return;

        editor.dataset.initialized = "true";
        this.initializePropertyCompletion(editor, scriptObjects);
        this.initializeScrollSync(editor);
        this.initializeHoverDocumentation(editor);
        editor.addEventListener("keydown", event => {
            if (event.isComposing) return;

            if (this.handleCompletionKey(editor, event)) return;

            if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
                event.preventDefault();
                return;
            }

            if ((event.ctrlKey || event.metaKey) &&
                event.key.toLowerCase() === "x" &&
                editor.selectionStart === editor.selectionEnd) {
                event.preventDefault();
                this.cutCurrentLine(editor);
                return;
            }

            if (event.altKey && !event.ctrlKey && !event.metaKey &&
                (event.key === "ArrowUp" || event.key === "ArrowDown")) {
                event.preventDefault();
                this.moveSelectedLines(editor, event.key === "ArrowUp" ? -1 : 1);
                return;
            }

            if (event.key === "{" && editor.selectionStart !== editor.selectionEnd) {
                event.preventDefault();
                this.wrapSelection(editor, "{", "}");
                return;
            }

            if (event.key !== "Tab") return;

            event.preventDefault();
            this.insertTab(editorId);
        });
    },

    initializeHoverDocumentation(editor) {
        const tooltip = document.getElementById("lexeme-tooltip");
        if (!tooltip) return;

        editor._hoverDocumentation = { tooltip, items: [], current: null, timer: 0 };
        editor.addEventListener("mousemove", event => this.updateHoverDocumentation(editor, event));
        editor.addEventListener("mouseleave", () => this.hideHoverDocumentation(editor));
        editor.addEventListener("scroll", () => this.hideHoverDocumentation(editor), { passive: true });
        editor.addEventListener("input", () => this.hideHoverDocumentation(editor));
    },

    setHoverDocumentation(editorId, items) {
        const editor = document.getElementById(editorId);
        const state = editor?._hoverDocumentation;
        if (!state) return;
        state.items = Array.isArray(items) ? items : [];
    },

    updateHoverDocumentation(editor, event) {
        const state = editor._hoverDocumentation;
        if (!state) return;

        const offset = this.sourceOffsetAt(editor, event.clientX, event.clientY);
        const item = offset < 0
            ? null
            : state.items.find(candidate => offset >= candidate.start && offset < candidate.start + candidate.length);
        if (item === state.current) return;

        clearTimeout(state.timer);
        state.timer = 0;
        state.current = item;
        state.tooltip.hidden = true;
        if (!item) return;

        state.timer = setTimeout(() => {
            if (state.current !== item) return;
            this.showHoverDocumentation(editor, item, event.clientX, event.clientY);
        }, 280);
    },

    sourceOffsetAt(editor, clientX, clientY) {
        const bounds = editor.getBoundingClientRect();
        const style = getComputedStyle(editor);
        const lineHeight = Number.parseFloat(style.lineHeight);
        const paddingLeft = Number.parseFloat(style.paddingLeft);
        const paddingTop = Number.parseFloat(style.paddingTop);
        if (!Number.isFinite(lineHeight) || lineHeight <= 0) return -1;

        const canvas = this._fontMeasureCanvas ??= document.createElement("canvas");
        const context = canvas.getContext("2d");
        context.font = style.font;
        const characterWidth = context.measureText("M").width;
        const x = clientX - bounds.left - paddingLeft + editor.scrollLeft;
        const y = clientY - bounds.top - paddingTop + editor.scrollTop;
        if (x < 0 || y < 0 || characterWidth <= 0) return -1;

        const lines = editor.value.split("\n");
        const lineIndex = Math.floor(y / lineHeight);
        if (lineIndex < 0 || lineIndex >= lines.length) return -1;
        const column = Math.floor(x / characterWidth);
        if (column < 0 || column >= lines[lineIndex].length) return -1;

        let offset = column;
        for (let index = 0; index < lineIndex; index++) offset += lines[index].length + 1;
        return offset;
    },

    showHoverDocumentation(editor, item, clientX, clientY) {
        const state = editor._hoverDocumentation;
        const tooltip = state.tooltip;
        const title = document.createElement("strong");
        title.className = "lexeme-tooltip-title";
        title.textContent = item.name;
        const signature = document.createElement("code");
        signature.className = "lexeme-tooltip-signature";
        signature.textContent = item.signature;
        const description = document.createElement("span");
        description.className = "lexeme-tooltip-description";
        description.textContent = item.description;
        const children = [title, signature, description];
        if (item.acceptedValues) {
            const values = document.createElement("span");
            values.className = "lexeme-tooltip-values";
            values.textContent = `Accepts: ${item.acceptedValues}`;
            children.push(values);
        }
        tooltip.replaceChildren(...children);
        tooltip.hidden = false;

        const surface = editor.closest(".code-surface");
        const bounds = surface.getBoundingClientRect();
        const tooltipBounds = tooltip.getBoundingClientRect();
        const left = Math.min(bounds.width - tooltipBounds.width - 8, Math.max(8, clientX - bounds.left + 14));
        const topBelow = clientY - bounds.top + 18;
        const top = topBelow + tooltipBounds.height <= bounds.height - 8
            ? topBelow
            : Math.max(8, clientY - bounds.top - tooltipBounds.height - 12);
        tooltip.style.left = `${left}px`;
        tooltip.style.top = `${top}px`;
    },

    hideHoverDocumentation(editor) {
        const state = editor?._hoverDocumentation;
        if (!state) return;
        clearTimeout(state.timer);
        state.timer = 0;
        state.current = null;
        state.tooltip.hidden = true;
    },

    initializeScrollSync(editor) {
        const highlights = document.getElementById("highlight-layer");
        if (!highlights) return;

        const synchronize = () => this.scheduleScrollSync(editor);
        editor.addEventListener("scroll", synchronize, { passive: true });
        editor.addEventListener("input", synchronize);

        const observer = new MutationObserver(synchronize);
        observer.observe(highlights, { childList: true, subtree: true, characterData: true });
        editor._highlightObserver = observer;

        if (window.ResizeObserver) {
            const resizeObserver = new ResizeObserver(synchronize);
            resizeObserver.observe(editor);
            editor._highlightResizeObserver = resizeObserver;
        }

        synchronize();
    },

    scheduleScrollSync(editor) {
        if (editor._scrollSyncFrame) cancelAnimationFrame(editor._scrollSyncFrame);
        editor._scrollSyncFrame = requestAnimationFrame(() => {
            editor._scrollSyncFrame = 0;
            this.syncScroll(editor);
        });
    },

    initializePropertyCompletion(editor, scriptObjects) {
        const surface = editor.closest(".code-surface");
        if (!surface) return;

        const popup = document.createElement("div");
        popup.className = "property-completion";
        popup.setAttribute("role", "listbox");
        popup.hidden = true;
        surface.appendChild(popup);
        editor._propertyCompletion = { popup, scriptObjects, matches: [], selected: 0, start: 0 };

        const refresh = () => this.refreshPropertyCompletion(editor);
        editor.addEventListener("input", refresh);
        editor.addEventListener("click", refresh);
        editor.addEventListener("keyup", event => {
            if (!["ArrowDown", "ArrowUp", "Enter", "Tab", "Escape"].includes(event.key)) refresh();
        });
        editor.addEventListener("blur", () => setTimeout(() => this.hidePropertyCompletion(editor), 120));
    },

    refreshPropertyCompletion(editor) {
        const state = editor._propertyCompletion;
        if (!state || editor.selectionStart !== editor.selectionEnd) return this.hidePropertyCompletion(editor);

        const caret = editor.selectionStart;
        const before = editor.value.slice(0, caret);
        const match = /\b([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)?$/.exec(before);
        if (!match) return this.hidePropertyCompletion(editor);

        const properties = state.scriptObjects[match[1]];
        if (!Array.isArray(properties)) return this.hidePropertyCompletion(editor);

        const prefix = match[2] ?? "";
        state.matches = properties.filter(name => name.startsWith(prefix));
        state.selected = Math.min(state.selected, Math.max(0, state.matches.length - 1));
        state.start = caret - prefix.length;
        if (!state.matches.length) return this.hidePropertyCompletion(editor);

        state.popup.replaceChildren(...state.matches.map((name, index) => {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "property-completion-item" + (index === state.selected ? " selected" : "");
            item.textContent = name;
            item.setAttribute("role", "option");
            item.setAttribute("aria-selected", index === state.selected ? "true" : "false");
            item.addEventListener("mousedown", event => {
                event.preventDefault();
                state.selected = index;
                this.acceptPropertyCompletion(editor);
            });
            return item;
        }));

        const lineStart = before.lastIndexOf("\n") + 1;
        const column = caret - lineStart;
        const line = before.slice(0, caret).split("\n").length - 1;
        state.popup.style.left = `${20 + column * 8.43 - editor.scrollLeft}px`;
        state.popup.style.top = `${20 + (line + 1) * 24 - editor.scrollTop}px`;
        state.popup.hidden = false;
    },

    handleCompletionKey(editor, event) {
        const state = editor._propertyCompletion;
        if (!state || state.popup.hidden) return false;

        if (event.key === "Escape") {
            event.preventDefault();
            this.hidePropertyCompletion(editor);
            return true;
        }
        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            const direction = event.key === "ArrowDown" ? 1 : -1;
            state.selected = (state.selected + direction + state.matches.length) % state.matches.length;
            this.refreshPropertyCompletion(editor);
            state.popup.querySelector(".selected")?.scrollIntoView({ block: "nearest" });
            return true;
        }
        if (event.key === "Enter" || event.key === "Tab") {
            event.preventDefault();
            this.acceptPropertyCompletion(editor);
            return true;
        }
        return false;
    },

    acceptPropertyCompletion(editor) {
        const state = editor._propertyCompletion;
        const property = state?.matches[state.selected];
        if (!property) return;

        editor.setRangeText(property, state.start, editor.selectionStart, "end");
        editor.dispatchEvent(new Event("input", { bubbles: true }));
        this.hidePropertyCompletion(editor);
        editor.focus();
    },

    hidePropertyCompletion(editor) {
        const state = editor._propertyCompletion;
        if (!state) return;
        state.popup.hidden = true;
        state.matches = [];
        state.selected = 0;
    },

    initializeSplitters(workspaceSelector) {
        const workspace = document.querySelector(workspaceSelector);
        if (!workspace || workspace.dataset.splittersInitialized) return;

        workspace.dataset.splittersInitialized = "true";
        const vertical = document.getElementById("editor-output-splitter");
        const horizontal = document.getElementById("main-diagnostics-splitter");
        const rightHorizontal = document.getElementById("viewer-output-splitter");
        this.initializeSplitter(workspace, vertical, "vertical");
        this.initializeSplitter(workspace, horizontal, "diagnostics");
        this.initializeSplitter(workspace, rightHorizontal, "output");
    },

    initializeSplitter(workspace, splitter, orientation) {
        if (!splitter) return;

        splitter.addEventListener("pointerdown", event => {
            event.preventDefault();
            splitter.setPointerCapture(event.pointerId);
            splitter.classList.add("dragging");
            document.body.classList.add("resizing-panes");

            const move = moveEvent => {
                this.resizePanes(workspace, orientation, moveEvent.clientX, moveEvent.clientY);
            };
            const stop = () => {
                splitter.classList.remove("dragging");
                document.body.classList.remove("resizing-panes");
                splitter.removeEventListener("pointermove", move);
                splitter.removeEventListener("pointerup", stop);
                splitter.removeEventListener("pointercancel", stop);
            };

            splitter.addEventListener("pointermove", move);
            splitter.addEventListener("pointerup", stop);
            splitter.addEventListener("pointercancel", stop);
        });

        splitter.addEventListener("keydown", event => {
            const valid = orientation === "vertical"
                ? event.key === "ArrowLeft" || event.key === "ArrowRight"
                : event.key === "ArrowUp" || event.key === "ArrowDown";
            if (!valid) return;

            event.preventDefault();
            const editor = workspace.querySelector(".editor-panel");
            const diagnostics = workspace.querySelector(".diagnostics-panel");
            const step = event.shiftKey ? 48 : 16;
            if (orientation === "vertical") {
                const direction = event.key === "ArrowLeft" ? -1 : 1;
                const x = workspace.getBoundingClientRect().left + editor.getBoundingClientRect().width + direction * step;
                this.resizePanes(workspace, orientation, x, 0);
            } else if (orientation === "diagnostics") {
                const direction = event.key === "ArrowUp" ? -1 : 1;
                const y = workspace.getBoundingClientRect().bottom - diagnostics.getBoundingClientRect().height + direction * step;
                this.resizePanes(workspace, orientation, 0, y);
            } else {
                const output = workspace.querySelector(".output-panel");
                const direction = event.key === "ArrowUp" ? -1 : 1;
                const y = output.getBoundingClientRect().top + direction * step;
                this.resizePanes(workspace, orientation, 0, y);
            }
        });
    },

    resizePanes(workspace, orientation, clientX, clientY) {
        const bounds = workspace.getBoundingClientRect();
        if (orientation === "vertical") {
            const minimum = Math.min(220, Math.max(120, bounds.width * .3));
            const editorWidth = Math.min(
                bounds.width - minimum - 10,
                Math.max(minimum, clientX - bounds.left));
            workspace.style.setProperty("--editor-width", editorWidth + "px");
            const percentage = Math.round(editorWidth / bounds.width * 100);
            document.getElementById("editor-output-splitter")?.setAttribute("aria-valuenow", percentage);
        } else if (orientation === "diagnostics") {
            const header = workspace.querySelector(".app-header");
            const availableHeight = bounds.bottom - header.getBoundingClientRect().bottom - 10;
            const minimum = Math.min(96, Math.max(64, availableHeight * .2));
            const maximum = Math.max(minimum, availableHeight - 140);
            const diagnosticsHeight = Math.min(
                maximum,
                Math.max(minimum, bounds.bottom - clientY));
            workspace.style.setProperty("--diagnostics-height", diagnosticsHeight + "px");
            const percentage = Math.round(diagnosticsHeight / availableHeight * 100);
            document.getElementById("main-diagnostics-splitter")?.setAttribute("aria-valuenow", percentage);
        } else {
            const viewer = workspace.querySelector(".viewer-panel");
            const outputBottom = workspace.querySelector("#main-diagnostics-splitter").getBoundingClientRect().top;
            const availableHeight = outputBottom - viewer.getBoundingClientRect().top - 10;
            const minimum = Math.min(80, Math.max(56, availableHeight * .18));
            const maximum = Math.max(minimum, availableHeight - 120);
            const outputHeight = Math.min(maximum, Math.max(minimum, outputBottom - clientY));
            workspace.style.setProperty("--output-height", outputHeight + "px");
            const percentage = Math.round(outputHeight / availableHeight * 100);
            document.getElementById("viewer-output-splitter")?.setAttribute("aria-valuenow", percentage);
        }

        const editor = document.getElementById("script-editor");
        if (editor) this.syncScroll(editor);
    },

    wrapSelection(editor, opening, closing) {
        const start = editor.selectionStart;
        const end = editor.selectionEnd;
        const selectedText = editor.value.slice(start, end);

        editor.setRangeText(opening + selectedText + closing, start, end, "end");
        editor.setSelectionRange(start + opening.length, end + opening.length);
        editor.dispatchEvent(new Event("input", { bubbles: true }));
    },

    cutCurrentLine(editor) {
        const lines = editor.value.split("\n");
        const lineIndex = this.lineIndexAt(editor.value, editor.selectionStart);
        const removed = lines[lineIndex];

        lines.splice(lineIndex, 1);
        editor.value = lines.join("\n");
        const caretLine = Math.min(lineIndex, Math.max(0, lines.length - 1));
        const caret = this.offsetForLine(lines, caretLine);
        editor.setSelectionRange(caret, caret);

        if (navigator.clipboard?.writeText) {
            navigator.clipboard.writeText(removed + "\n").catch(() => {});
        }

        editor.dispatchEvent(new Event("input", { bubbles: true }));
        this.syncScroll(editor);
    },

    moveSelectedLines(editor, direction) {
        const value = editor.value;
        const selectionStart = editor.selectionStart;
        const selectionEnd = editor.selectionEnd;
        const lines = value.split("\n");
        const startLine = this.lineIndexAt(value, selectionStart);
        let endLine = this.lineIndexAt(value, selectionEnd);

        if (selectionEnd > selectionStart && value[selectionEnd - 1] === "\n") {
            endLine--;
        }

        if ((direction < 0 && startLine === 0) ||
            (direction > 0 && endLine === lines.length - 1)) {
            return;
        }

        const oldBlockStart = this.offsetForLine(lines, startLine);
        const relativeStart = selectionStart - oldBlockStart;
        const relativeEnd = selectionEnd - oldBlockStart;
        const count = endLine - startLine + 1;
        const block = lines.splice(startLine, count);
        const newStartLine = startLine + direction;
        lines.splice(newStartLine, 0, ...block);

        editor.value = lines.join("\n");
        const newBlockStart = this.offsetForLine(lines, newStartLine);
        editor.setSelectionRange(
            newBlockStart + relativeStart,
            newBlockStart + relativeEnd);
        editor.dispatchEvent(new Event("input", { bubbles: true }));
        this.syncScroll(editor);
    },

    lineIndexAt(value, position) {
        return value.slice(0, position).split("\n").length - 1;
    },

    offsetForLine(lines, lineIndex) {
        let offset = 0;
        for (let index = 0; index < lineIndex; index++) {
            offset += lines[index].length + 1;
        }

        return offset;
    },

    syncScroll(editor) {
        if (!editor) return;
        const gutter = document.getElementById("line-numbers");
        if (gutter) gutter.scrollTop = editor.scrollTop;

        const highlights = document.getElementById("highlight-layer");
        if (highlights) {
            if (highlights.scrollTop !== editor.scrollTop) highlights.scrollTop = editor.scrollTop;
            if (highlights.scrollLeft !== editor.scrollLeft) highlights.scrollLeft = editor.scrollLeft;
        }
    },

    selectRange(editorId, start, length) {
        const editor = document.getElementById(editorId);
        if (!editor) return;

        editor.focus();
        editor.setSelectionRange(start, start + length);

        const lineHeight = parseFloat(getComputedStyle(editor).lineHeight) || 24;
        const line = editor.value.slice(0, start).split("\n").length;
        editor.scrollTop = Math.max(0, (line - 3) * lineHeight);
        this.syncScroll(editor);
    },

    insertTab(editorId) {
        const editor = document.getElementById(editorId);
        if (!editor) return;

        const start = editor.selectionStart;
        const end = editor.selectionEnd;
        editor.value = editor.value.slice(0, start) + "  " + editor.value.slice(end);
        editor.setSelectionRange(start + 2, start + 2);
        editor.dispatchEvent(new Event("input", { bubbles: true }));
    }
};

window.dockInterop = {
    storageKey: "triugla-dock-layout-v3",
    dockNames: ["top", "left", "center", "right", "bottom"],
    defaultDocks: {
        top: [],
        left: [],
        center: ["editor"],
        right: ["viewer"],
        bottom: ["diagnostics", "output"]
    },
    defaultActive: { center: "editor", right: "viewer", bottom: "diagnostics" },
    active: {},

    initialize(workspaceSelector) {
        const workspace = document.querySelector(workspaceSelector);
        const layout = workspace?.querySelector(".dock-layout");
        if (!layout || layout.dataset.dockInitialized) return;

        layout.dataset.dockInitialized = "true";
        this.workspace = workspace;
        this.layout = layout;
        this.applyDefaultLayout();
        this.restore();
        this.createDropOverlay();
        this.renderAllTabs();
        layout.querySelectorAll("[data-resize-dock]").forEach(splitter =>
            this.initializeDockSplitter(splitter, splitter.dataset.resizeDock));
    },

    host(name) {
        return this.layout?.querySelector(`[data-dock="${name}"]`);
    },

    panels(name) {
        return Array.from(this.host(name)?.querySelectorAll(":scope > .dock-content > .dock-panel") ?? []);
    },

    applyDefaultLayout() {
        Object.entries(this.defaultDocks).forEach(([name, ids]) => {
            const content = this.host(name)?.querySelector(":scope > .dock-content");
            if (!content) return;
            ids.forEach(id => {
                const panel = this.layout.querySelector(`.dock-panel[data-panel-id="${id}"]`);
                if (panel) content.appendChild(panel);
            });
        });
        this.active = { ...this.defaultActive };
        ["--left-width", "--right-width", "--top-height", "--bottom-height"].forEach(property =>
            this.layout.style.removeProperty(property));
    },

    renderAllTabs() {
        this.dockNames.forEach(name => this.renderTabs(name));
        this.dockNames.forEach(name =>
            this.layout.classList.toggle(`has-${name}`, this.panels(name).length > 0));
        this.layout.classList.remove(
            "fill-center-left",
            "fill-center-right",
            "fill-center-top",
            "fill-center-bottom");
        if (this.panels("center").length === 0) {
            const fillDock = ["left", "right", "top", "bottom"]
                .find(name => this.panels(name).length > 0);
            if (fillDock) this.layout.classList.add(`fill-center-${fillDock}`);
        }
        window.dispatchEvent(new Event("resize"));
    },

    renderTabs(name) {
        const host = this.host(name);
        if (!host) return;

        const panels = this.panels(name);
        const tabs = host.querySelector(":scope > .dock-tabs");
        tabs.replaceChildren();
        host.classList.toggle("dock-empty", panels.length === 0);
        if (panels.length === 0) return;

        if (!panels.some(panel => panel.dataset.panelId === this.active[name])) {
            this.active[name] = panels[0].dataset.panelId;
        }

        panels.forEach(panel => {
            const id = panel.dataset.panelId;
            const selected = id === this.active[name];
            panel.id ||= `dock-panel-${id}`;
            panel.hidden = !selected;
            panel.setAttribute("role", "tabpanel");
            panel.setAttribute("aria-labelledby", `dock-tab-${id}`);

            const tab = document.createElement("button");
            tab.type = "button";
            tab.id = `dock-tab-${id}`;
            tab.className = `dock-tab${selected ? " active" : ""}`;
            tab.dataset.panelId = id;
            tab.draggable = false;
            tab.setAttribute("role", "tab");
            tab.setAttribute("aria-selected", String(selected));
            tab.setAttribute("aria-controls", panel.id);
            tab.setAttribute("aria-label", `${panel.dataset.panelTitle} panel. Drag to dock or reorder. Use Alt+Shift with an arrow key to dock, Alt+Shift+Enter for center, or Control+Shift+Left and Right to reorder.`);
            tab.title = "Drag to dock or reorder · Alt+Shift+arrows move · Ctrl+Shift+arrows reorder";
            tab.tabIndex = selected ? 0 : -1;
            tab.innerHTML = `<span class="dock-tab-grip" aria-hidden="true"></span><span>${panel.dataset.panelTitle}</span>`;
            tab.addEventListener("click", () => {
                if (this.suppressClickId === id) {
                    this.suppressClickId = null;
                    return;
                }
                this.activate(name, id);
            });
            tab.addEventListener("keydown", event => this.handleTabKey(event, name, id));
            tab.addEventListener("pointerdown", event => this.startPointerDrag(event, id, panel.dataset.panelTitle));
            tabs.appendChild(tab);

            const panelBar = panel.querySelector(":scope > .panel-bar");
            if (panelBar && !panelBar.dataset.dockDragInitialized) {
                panelBar.dataset.dockDragInitialized = "true";
                panelBar.classList.add("dock-drag-handle");
                panelBar.addEventListener("pointerdown", event => {
                    if (event.target.closest("button, input, textarea, select, a, [role='button']")) return;
                    this.startPointerDrag(event, id, panel.dataset.panelTitle);
                });
            }
        });
    },

    activate(name, id) {
        this.active[name] = id;
        this.renderTabs(name);
        this.save();
        requestAnimationFrame(() => window.dispatchEvent(new Event("resize")));
    },

    reveal(id) {
        const panel = this.layout?.querySelector(`.dock-panel[data-panel-id="${id}"]`);
        const host = panel?.closest(".dock-host");
        if (!host) return;
        this.activate(host.dataset.dock, id);
    },

    handleTabKey(event, name, id) {
        if (event.ctrlKey && event.shiftKey && ["ArrowLeft", "ArrowRight"].includes(event.key)) {
            event.preventDefault();
            const panels = this.panels(name);
            const currentIndex = panels.findIndex(panel => panel.dataset.panelId === id);
            const targetIndex = event.key === "ArrowLeft" ? currentIndex - 1 : currentIndex + 1;
            if (currentIndex >= 0 && targetIndex >= 0 && targetIndex < panels.length) {
                this.movePanel(id, name, targetIndex);
                requestAnimationFrame(() => this.host(name)?.querySelector(`.dock-tab[data-panel-id="${id}"]`)?.focus());
            }
            return;
        }

        if (event.altKey && event.shiftKey) {
            const target = event.key === "ArrowLeft"
                ? "left"
                : event.key === "ArrowRight"
                    ? "right"
                    : event.key === "ArrowUp"
                        ? "top"
                    : event.key === "ArrowDown"
                        ? "bottom"
                        : event.key === "Enter"
                            ? "center"
                        : null;
            if (target) {
                event.preventDefault();
                this.movePanel(id, target);
                requestAnimationFrame(() => this.host(target)?.querySelector(`.dock-tab[data-panel-id="${id}"]`)?.focus());
                return;
            }
        }

        const tabs = Array.from(this.host(name).querySelectorAll(".dock-tab"));
        const index = tabs.findIndex(tab => tab.dataset.panelId === id);
        let next = null;
        if (event.key === "ArrowLeft") next = tabs[(index - 1 + tabs.length) % tabs.length];
        if (event.key === "ArrowRight") next = tabs[(index + 1) % tabs.length];
        if (event.key === "Home") next = tabs[0];
        if (event.key === "End") next = tabs[tabs.length - 1];
        if (!next) return;
        event.preventDefault();
        this.activate(name, next.dataset.panelId);
        next.focus();
    },

    createDropOverlay() {
        const overlay = document.createElement("div");
        overlay.className = "dock-drop-overlay";
        overlay.setAttribute("aria-hidden", "true");
        const labels = {
            top: "Snap top",
            left: "Snap left",
            center: "Snap center",
            right: "Snap right",
            bottom: "Snap bottom"
        };
        Object.entries(labels).forEach(([name, label]) => {
            const target = document.createElement("div");
            target.className = `dock-drop-target drop-${name}`;
            target.dataset.targetDock = name;
            target.innerHTML = `<span>${label}</span>`;
            target.addEventListener("dragover", event => {
                event.preventDefault();
                event.dataTransfer.dropEffect = "move";
                target.classList.add("drag-over");
            });
            target.addEventListener("dragleave", () => target.classList.remove("drag-over"));
            target.addEventListener("drop", event => {
                event.preventDefault();
                this.movePanel(event.dataTransfer.getData("text/plain"), name);
            });
            overlay.appendChild(target);
        });
        this.layout.appendChild(overlay);
        this.overlay = overlay;
    },

    startDrag(event, id) {
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", id);
        this.draggedPanel = id;
        this.overlay.classList.add("visible");
        document.body.classList.add("docking-panel");
    },

    startPointerDrag(event, id, title) {
        if (event.button !== 0 || this.pointerDrag) return;

        const originX = event.clientX;
        const originY = event.clientY;
        const pointerId = event.pointerId;
        this.pointerDrag = { id, title, originX, originY, active: false, target: null };

        const move = moveEvent => {
            if (moveEvent.pointerId !== pointerId || !this.pointerDrag) return;
            const distance = Math.hypot(moveEvent.clientX - originX, moveEvent.clientY - originY);
            if (!this.pointerDrag.active && distance < 6) return;

            if (!this.pointerDrag.active) {
                this.pointerDrag.active = true;
                this.suppressClickId = id;
                this.overlay.classList.add("visible");
                document.body.classList.add("docking-panel");
                this.createDragGhost(title);
            }

            moveEvent.preventDefault();
            this.positionDragGhost(moveEvent.clientX, moveEvent.clientY);
            const destination = this.dropDestinationAt(moveEvent.clientX, moveEvent.clientY, id);
            this.showDropDestination(destination);
            this.pointerDrag.target = destination;
        };

        const stop = stopEvent => {
            if (stopEvent.pointerId !== pointerId) return;
            window.removeEventListener("pointermove", move);
            window.removeEventListener("pointerup", stop);
            window.removeEventListener("pointercancel", cancel);
            const drag = this.pointerDrag;
            this.pointerDrag = null;
            const target = drag?.active
                ? this.dropDestinationAt(stopEvent.clientX, stopEvent.clientY, drag.id) ?? drag.target
                : null;
            if (drag?.active && target) {
                this.movePanel(drag.id, target.dock, target.index);
            } else {
                this.endDrag();
            }
            window.setTimeout(() => {
                if (this.suppressClickId === drag?.id) this.suppressClickId = null;
            }, 0);
        };

        const cancel = cancelEvent => {
            if (cancelEvent.pointerId !== pointerId) return;
            window.removeEventListener("pointermove", move);
            window.removeEventListener("pointerup", stop);
            window.removeEventListener("pointercancel", cancel);
            this.pointerDrag = null;
            this.endDrag();
        };

        window.addEventListener("pointermove", move, { passive: false });
        window.addEventListener("pointerup", stop);
        window.addEventListener("pointercancel", cancel);
    },

    snapTargetAt(x, y) {
        const bounds = this.layout.getBoundingClientRect();
        if (x < bounds.left || x > bounds.right || y < bounds.top || y > bounds.bottom) return null;

        const horizontal = (x - bounds.left) / bounds.width;
        const vertical = (y - bounds.top) / bounds.height;
        if (vertical <= .2) return "top";
        if (vertical >= .8) return "bottom";
        if (horizontal <= .22) return "left";
        if (horizontal >= .78) return "right";
        return "center";
    },

    dropDestinationAt(x, y, draggedId) {
        const bounds = this.layout.getBoundingClientRect();
        if (x < bounds.left || x > bounds.right || y < bounds.top || y > bounds.bottom) return null;

        for (const name of this.dockNames) {
            const host = this.host(name);
            const tabs = host?.querySelector(":scope > .dock-tabs");
            if (!tabs || getComputedStyle(host).visibility === "hidden") continue;
            const tabBounds = tabs.getBoundingClientRect();
            if (x < tabBounds.left || x > tabBounds.right || y < tabBounds.top || y > tabBounds.bottom) continue;

            const candidates = Array.from(tabs.querySelectorAll(".dock-tab"))
                .filter(tab => tab.dataset.panelId !== draggedId);
            const index = candidates.findIndex(tab => x < tab.getBoundingClientRect().left + tab.getBoundingClientRect().width / 2);
            return { dock: name, index: index < 0 ? candidates.length : index };
        }

        return { dock: this.snapTargetAt(x, y), index: null };
    },

    showDropDestination(destination) {
        this.overlay.querySelectorAll(".drag-over").forEach(item => item.classList.remove("drag-over"));
        this.layout.querySelectorAll(".dock-tab-insert, .dock-tabs-insert-end").forEach(item =>
            item.classList.remove("dock-tab-insert", "dock-tabs-insert-end"));
        if (!destination?.dock) return;

        const target = this.overlay.querySelector(`[data-target-dock="${destination.dock}"]`);
        target?.classList.add("drag-over");
        if (destination.index === null) return;

        const tabs = this.host(destination.dock)?.querySelector(":scope > .dock-tabs");
        const candidates = Array.from(tabs?.querySelectorAll(".dock-tab") ?? [])
            .filter(tab => tab.dataset.panelId !== this.pointerDrag?.id);
        if (destination.index < candidates.length) {
            candidates[destination.index].classList.add("dock-tab-insert");
        } else {
            tabs?.classList.add("dock-tabs-insert-end");
        }
    },

    createDragGhost(title) {
        this.dragGhost?.remove();
        const ghost = document.createElement("div");
        ghost.className = "dock-drag-ghost";
        ghost.textContent = title;
        document.body.appendChild(ghost);
        this.dragGhost = ghost;
    },

    positionDragGhost(x, y) {
        if (!this.dragGhost) return;
        this.dragGhost.style.transform = `translate3d(${x + 12}px, ${y + 12}px, 0)`;
    },

    endDrag() {
        this.overlay?.classList.remove("visible");
        this.overlay?.querySelectorAll(".drag-over").forEach(item => item.classList.remove("drag-over"));
        this.layout?.querySelectorAll(".dock-tab-insert, .dock-tabs-insert-end").forEach(item =>
            item.classList.remove("dock-tab-insert", "dock-tabs-insert-end"));
        document.body.classList.remove("docking-panel");
        this.dragGhost?.remove();
        this.dragGhost = null;
        this.draggedPanel = null;
    },

    movePanel(id, targetName, targetIndex = null) {
        const panel = this.layout.querySelector(`.dock-panel[data-panel-id="${id}"]`);
        const target = this.host(targetName)?.querySelector(":scope > .dock-content");
        if (!panel || !target) return;
        const candidates = Array.from(target.querySelectorAll(":scope > .dock-panel"))
            .filter(candidate => candidate !== panel);
        const reference = targetIndex === null ? null : candidates[targetIndex] ?? null;
        target.insertBefore(panel, reference);
        this.active[targetName] = id;
        this.endDrag();
        this.renderAllTabs();
        this.save();
    },

    initializeDockSplitter(splitter, dockName) {
        if (!splitter) return;
        const resize = event => {
            const bounds = this.layout.getBoundingClientRect();
            const minimumWidth = Math.min(180, bounds.width * .22);
            const maximumWidth = Math.max(minimumWidth, bounds.width * .55);
            const minimumHeight = Math.min(96, bounds.height * .2);
            const maximumHeight = Math.max(minimumHeight, bounds.height * .55);
            if (dockName === "left")
                this.layout.style.setProperty("--left-width", `${Math.max(minimumWidth, Math.min(maximumWidth, event.clientX - bounds.left))}px`);
            if (dockName === "right")
                this.layout.style.setProperty("--right-width", `${Math.max(minimumWidth, Math.min(maximumWidth, bounds.right - event.clientX))}px`);
            if (dockName === "top")
                this.layout.style.setProperty("--top-height", `${Math.max(minimumHeight, Math.min(maximumHeight, event.clientY - bounds.top))}px`);
            if (dockName === "bottom")
                this.layout.style.setProperty("--bottom-height", `${Math.max(minimumHeight, Math.min(maximumHeight, bounds.bottom - event.clientY))}px`);
            window.dispatchEvent(new Event("resize"));
        };
        splitter.addEventListener("pointerdown", event => {
            event.preventDefault();
            splitter.setPointerCapture(event.pointerId);
            splitter.classList.add("dragging");
            document.body.classList.add("resizing-panes");
            const move = moveEvent => resize(moveEvent);
            const stop = () => {
                splitter.classList.remove("dragging");
                document.body.classList.remove("resizing-panes");
                splitter.removeEventListener("pointermove", move);
                splitter.removeEventListener("pointerup", stop);
                this.save();
            };
            splitter.addEventListener("pointermove", move);
            splitter.addEventListener("pointerup", stop);
        });
        splitter.addEventListener("keydown", event => {
            const horizontal = dockName === "left" || dockName === "right";
            const valid = horizontal
                ? ["ArrowLeft", "ArrowRight"].includes(event.key)
                : ["ArrowUp", "ArrowDown"].includes(event.key);
            if (!valid) return;
            event.preventDefault();
            const bounds = splitter.getBoundingClientRect();
            const step = event.shiftKey ? 48 : 16;
            const xDirection = event.key === "ArrowLeft" ? -step : step;
            const yDirection = event.key === "ArrowUp" ? -step : step;
            resize({
                clientX: (dockName === "right" ? bounds.right : bounds.left) + xDirection,
                clientY: (dockName === "bottom" ? bounds.bottom : bounds.top) + yDirection
            });
            this.save();
        });
    },

    save() {
        if (!this.layout) return;
        const docks = {};
        this.dockNames.forEach(name => {
            docks[name] = this.panels(name).map(panel => panel.dataset.panelId);
        });
        try {
            localStorage.setItem(this.storageKey, JSON.stringify({
                docks,
                active: this.active,
                leftWidth: this.layout.style.getPropertyValue("--left-width"),
                rightWidth: this.layout.style.getPropertyValue("--right-width"),
                topHeight: this.layout.style.getPropertyValue("--top-height"),
                bottomHeight: this.layout.style.getPropertyValue("--bottom-height")
            }));
        } catch { }
    },

    restore() {
        let state;
        try { state = JSON.parse(localStorage.getItem(this.storageKey)); } catch { return; }
        if (!state?.docks) return;
        const known = new Set(Array.from(this.layout.querySelectorAll(".dock-panel"), panel => panel.dataset.panelId));
        Object.entries(state.docks).forEach(([name, ids]) => {
            const content = this.host(name)?.querySelector(":scope > .dock-content");
            if (!content || !Array.isArray(ids)) return;
            ids.filter(id => known.has(id)).forEach(id => {
                content.appendChild(this.layout.querySelector(`.dock-panel[data-panel-id="${id}"]`));
            });
        });
        this.active = { ...this.active, ...state.active };
        if (state.leftWidth) this.layout.style.setProperty("--left-width", state.leftWidth);
        if (state.rightWidth) this.layout.style.setProperty("--right-width", state.rightWidth);
        if (state.topHeight) this.layout.style.setProperty("--top-height", state.topHeight);
        if (state.bottomHeight) this.layout.style.setProperty("--bottom-height", state.bottomHeight);
    }
};
