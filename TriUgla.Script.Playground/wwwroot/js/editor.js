window.editorInterop = {
    initializeGridSplitters(layoutSelector) {
        const layout = document.querySelector(layoutSelector);
        if (!layout || layout.dataset.gridSplittersInitialized)
            return;
        layout.dataset.gridSplittersInitialized = "true";
        this.initializeGridSplitter(layout, document.getElementById("column-splitter"), "columns");
        this.initializeGridSplitter(layout, document.getElementById("left-row-splitter"), "leftRows");
        this.initializeGridSplitter(layout, document.getElementById("right-row-splitter"), "rightRows");
    },
    initializeGridSplitter(layout, splitter, axis) {
        if (!splitter)
            return;
        const resize = (clientX, clientY) => this.resizeGrid(layout, splitter, axis, clientX, clientY);
        splitter.addEventListener("pointerdown", event => {
            if (event.button !== 0)
                return;
            event.preventDefault();
            splitter.setPointerCapture(event.pointerId);
            splitter.classList.add("is-resizing");
            document.body.style.userSelect = "none";
            const move = moveEvent => resize(moveEvent.clientX, moveEvent.clientY);
            const stop = () => {
                splitter.classList.remove("is-resizing");
                document.body.style.removeProperty("user-select");
                splitter.removeEventListener("pointermove", move);
                splitter.removeEventListener("pointerup", stop);
                splitter.removeEventListener("pointercancel", stop);
            };
            splitter.addEventListener("pointermove", move);
            splitter.addEventListener("pointerup", stop);
            splitter.addEventListener("pointercancel", stop);
        });
        splitter.addEventListener("keydown", event => {
            const horizontalKey = event.key === "ArrowLeft" || event.key === "ArrowRight";
            const verticalKey = event.key === "ArrowUp" || event.key === "ArrowDown";
            if (axis === "columns" ? !horizontalKey : !verticalKey)
                return;
            event.preventDefault();
            const bounds = splitter.getBoundingClientRect();
            const step = event.shiftKey ? 48 : 16;
            const x = bounds.left + bounds.width / 2 + (event.key === "ArrowLeft" ? -step : event.key === "ArrowRight" ? step : 0);
            const y = bounds.top + bounds.height / 2 + (event.key === "ArrowUp" ? -step : event.key === "ArrowDown" ? step : 0);
            resize(x, y);
        });
    },
    resizeGrid(layout, splitter, axis, clientX, clientY) {
        if (axis === "columns") {
            const bounds = layout.getBoundingClientRect();
            const usable = Math.max(1, bounds.width - splitter.offsetWidth);
            const left = Math.min(usable * .75, Math.max(usable * .25, clientX - bounds.left));
            layout.style.setProperty("--left-column-width", left + "px");
            splitter.setAttribute("aria-valuenow", Math.round(left / usable * 100));
        }
        else {
            const column = splitter.closest(".workspace-column");
            const bounds = column.getBoundingClientRect();
            const usable = Math.max(1, bounds.height - splitter.offsetHeight);
            const top = Math.min(usable * .8, Math.max(usable * .2, clientY - bounds.top));
            const bottom = usable - top;
            column.style.setProperty("--bottom-panel-height", bottom + "px");
            splitter.setAttribute("aria-valuenow", Math.round(top / usable * 100));
        }
        const editor = document.getElementById("script-editor");
        if (editor)
            this.syncScroll(editor);
        window.dispatchEvent(new Event("resize"));
    },
    openScriptFile() {
        return new Promise(resolve => {
            const input = document.createElement("input");
            input.type = "file";
            input.accept = ".tus,.geo,.txt,text/plain";
            input.addEventListener("change", async () => {
                const file = input.files?.[0];
                if (!file) {
                    resolve(null);
                    return;
                }
                resolve({ name: file.name, content: await file.text() });
            }, { once: true });
            input.addEventListener("cancel", () => resolve(null), { once: true });
            input.click();
        });
    },
    async saveScriptFile(suggestedName, content) {
        const fileName = suggestedName || "script.tus";
        if (window.showSaveFilePicker) {
            try {
                const handle = await window.showSaveFilePicker({
                    suggestedName: fileName,
                    types: [{
                        description: "TriUgla script",
                        accept: { "text/plain": [".tus", ".geo", ".txt"] }
                    }]
                });
                const writable = await handle.createWritable();
                await writable.write(content);
                await writable.close();
                return handle.name;
            }
            catch (error) {
                if (error?.name === "AbortError")
                    return null;
                throw error;
            }
        }

        const blob = new Blob([content], { type: "text/plain;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        link.style.display = "none";
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 0);
        return fileName;
    },
    initialize(editorId, scriptObjects = {}) {
        const editor = document.getElementById(editorId);
        if (!editor || editor.dataset.initialized)
            return;
        editor.dataset.initialized = "true";
        this.initializeEditorHistory(editor);
        this.initializePropertyCompletion(editor, scriptObjects);
        this.initializeScrollSync(editor);
        this.initializeHoverDocumentation(editor);
        editor.addEventListener("dblclick", () => this.selectWordAtCaret(editor));
        editor.addEventListener("keydown", event => {
            if (event.isComposing)
                return;
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "z") {
                event.preventDefault();
                this.moveEditorHistory(editor, event.shiftKey ? 1 : -1);
                return;
            }
            if (event.ctrlKey && !event.metaKey && event.key.toLowerCase() === "y") {
                event.preventDefault();
                this.moveEditorHistory(editor, 1);
                return;
            }
            if (this.handleCompletionKey(editor, event))
                return;
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
            if (event.key !== "Tab")
                return;
            event.preventDefault();
            this.changeIndentation(editorId, event.shiftKey);
        });
    },
    initializeEditorHistory(editor) {
        editor._history = {
            entries: [this.editorSnapshot(editor)],
            index: 0,
            applying: false
        };
        editor.addEventListener("input", () => this.recordEditorHistory(editor));
    },
    editorSnapshot(editor) {
        return {
            value: editor.value,
            start: editor.selectionStart,
            end: editor.selectionEnd
        };
    },
    recordEditorHistory(editor) {
        const history = editor._history;
        if (!history || history.applying)
            return;
        const snapshot = this.editorSnapshot(editor);
        const current = history.entries[history.index];
        if (current?.value === snapshot.value) {
            history.entries[history.index] = snapshot;
            return;
        }
        history.entries.splice(history.index + 1);
        history.entries.push(snapshot);
        if (history.entries.length > 500)
            history.entries.shift();
        history.index = history.entries.length - 1;
    },
    resetEditorHistory(editorId) {
        const editor = document.getElementById(editorId);
        if (!editor)
            return;
        editor._history = {
            entries: [this.editorSnapshot(editor)],
            index: 0,
            applying: false
        };
    },
    replaceEditorContent(editorId, value) {
        const editor = document.getElementById(editorId);
        if (!editor)
            return;
        editor.value = value ?? "";
        editor.setSelectionRange(0, 0);
        this.scheduleEditorLayout(editor);
        this.syncScroll(editor);
        this.resetEditorHistory(editorId);
    },
    moveEditorHistory(editor, direction) {
        const history = editor._history;
        if (!history)
            return;
        this.recordEditorHistory(editor);
        const target = Math.max(0, Math.min(history.entries.length - 1, history.index + direction));
        if (target === history.index)
            return;
        history.index = target;
        const snapshot = history.entries[target];
        history.applying = true;
        editor.value = snapshot.value;
        editor.setSelectionRange(snapshot.start, snapshot.end);
        editor.dispatchEvent(new Event("input", { bubbles: true }));
        history.applying = false;
        editor.focus({ preventScroll: true });
        this.syncScroll(editor);
    },
    selectWordAtCaret(editor) {
        const value = editor.value;
        if (!value)
            return;
        let position = editor.selectionStart;
        if (position >= value.length)
            position = value.length - 1;
        const isWordCharacter = character => /[\p{L}\p{N}_]/u.test(character);
        if (!isWordCharacter(value[position])) {
            if (position === 0 || !isWordCharacter(value[position - 1]))
                return;
            position--;
        }
        let start = position;
        let end = position + 1;
        while (start > 0 && isWordCharacter(value[start - 1])) start--;
        while (end < value.length && isWordCharacter(value[end])) end++;
        editor.setSelectionRange(start, end);
    },
    initializeHoverDocumentation(editor) {
        const tooltip = document.createElement("div");
        tooltip.className = "lexeme-tooltip";
        tooltip.setAttribute("role", "tooltip");
        tooltip.hidden = true;
        document.body.appendChild(tooltip);
        editor._hoverDocumentation = {
            tooltip,
            items: [],
            hitRanges: [],
            current: null,
            timer: 0
        };
        editor.addEventListener("mousemove", event => this.updateHoverDocumentation(editor, event));
        editor.addEventListener("mouseleave", () => this.hideHoverDocumentation(editor));
        editor.addEventListener("scroll", () => this.hideHoverDocumentation(editor), { passive: true });
        editor.addEventListener("input", () => this.hideHoverDocumentation(editor));
    },
    setHoverDocumentation(editorId, items) {
        const editor = document.getElementById(editorId);
        const state = editor?._hoverDocumentation;
        if (!state)
            return;
        state.items = Array.isArray(items) ? items : [];
        state.hitRanges = this.documentationHitRanges(editor, state.items);
    },
    updateHoverDocumentation(editor, event) {
        const state = editor._hoverDocumentation;
        if (!state)
            return;
        let hit = null;
        let hitRect = null;
        for (const candidate of state.hitRanges) {
            const rect = [...candidate.range.getClientRects()].find(fragment =>
                event.clientX >= fragment.left && event.clientX <= fragment.right &&
                event.clientY >= fragment.top && event.clientY <= fragment.bottom);
            if (rect) {
                hit = candidate;
                hitRect = rect;
                break;
            }
        }
        const item = hit?.item ?? null;
        if (item === state.current)
            return;
        clearTimeout(state.timer);
        state.timer = 0;
        state.current = item;
        state.tooltip.hidden = true;
        if (!item)
            return;
        state.timer = setTimeout(() => {
            if (state.current !== item)
                return;
            this.showHoverDocumentation(editor, item, hitRect);
        }, 280);
    },
    documentationHitRanges(editor, items) {
        const root = editor.closest(".code-surface")?.querySelector("#highlight-code");
        if (!root)
            return [];
        return items.map(item => ({ item, range: this.textRange(root, item.start, item.length) }))
            .filter(candidate => candidate.range);
    },
    textRange(root, start, length) {
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
        const range = document.createRange();
        let offset = 0;
        let startNode = null;
        let startOffset = 0;
        let endNode = null;
        let endOffset = 0;
        const end = start + length;
        while (walker.nextNode()) {
            const node = walker.currentNode;
            const next = offset + node.data.length;
            if (!startNode && start >= offset && start <= next) {
                startNode = node;
                startOffset = Math.min(node.data.length, start - offset);
            }
            if (end >= offset && end <= next) {
                endNode = node;
                endOffset = Math.min(node.data.length, end - offset);
                break;
            }
            offset = next;
        }
        if (!startNode || !endNode)
            return null;
        range.setStart(startNode, startOffset);
        range.setEnd(endNode, endOffset);
        return range;
    },
    showHoverDocumentation(editor, item, anchorRect) {
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
        const tooltipBounds = tooltip.getBoundingClientRect();
        const anchorLeft = anchorRect?.left ?? 8;
        const anchorRight = anchorRect?.right ?? anchorLeft;
        const anchorTop = anchorRect?.top ?? 8;
        const anchorBottom = anchorRect?.bottom ?? anchorTop;
        const left = Math.min(
            window.innerWidth - tooltipBounds.width - 8,
            Math.max(8, anchorRight + 10));
        const topBelow = anchorBottom + 8;
        const top = topBelow + tooltipBounds.height <= window.innerHeight - 8
            ? topBelow
            : Math.max(8, anchorTop - tooltipBounds.height - 8);
        tooltip.style.left = `${left}px`;
        tooltip.style.top = `${top}px`;
    },
    hideHoverDocumentation(editor) {
        const state = editor?._hoverDocumentation;
        if (!state)
            return;
        clearTimeout(state.timer);
        state.timer = 0;
        state.current = null;
        state.tooltip.hidden = true;
    },
    initializeScrollSync(editor) {
        const highlights = document.getElementById("highlight-layer");
        if (!highlights)
            return;
        const synchronizeScroll = () => this.scheduleScrollSync(editor);
        const synchronizeLayout = () => {
            this.scheduleEditorLayout(editor);
            this.scheduleScrollSync(editor);
        };
        editor.addEventListener("scroll", synchronizeScroll, { passive: true });
        editor.addEventListener("input", synchronizeLayout);
        const observer = new MutationObserver(synchronizeLayout);
        observer.observe(highlights, { childList: true, subtree: true, characterData: true });
        editor._highlightObserver = observer;
        if (window.ResizeObserver) {
            const resizeObserver = new ResizeObserver(synchronizeLayout);
            resizeObserver.observe(editor);
            editor._highlightResizeObserver = resizeObserver;
        }
        synchronizeLayout();
    },
    scheduleEditorLayout(editor) {
        if (editor._layoutFrame)
            cancelAnimationFrame(editor._layoutFrame);
        editor._layoutFrame = requestAnimationFrame(() => {
            editor._layoutFrame = 0;
            this.updateEditorLayout(editor);
        });
    },
    updateEditorLayout(editor) {
        const surface = editor.closest(".code-surface");
        const lineNumbers = document.getElementById("line-number-content");
        if (!surface || !lineNumbers)
            return;
        const style = getComputedStyle(editor);
        const paddingLeft = Number.parseFloat(style.paddingLeft) || 0;
        const paddingRight = Number.parseFloat(style.paddingRight) || 0;
        const contentWidth = Math.max(1, editor.clientWidth - paddingLeft - paddingRight);
        const scrollbarWidth = Math.max(0, editor.offsetWidth - editor.clientWidth);
        surface.style.setProperty("--editor-scrollbar-width", `${scrollbarWidth}px`);
        const highlights = surface.querySelector("#highlight-code");
        if (highlights)
            highlights.style.width = `${contentWidth}px`;
        let measure = editor._lineMeasure;
        if (!measure) {
            measure = document.createElement("div");
            measure.className = "editor-line-measure";
            surface.appendChild(measure);
            editor._lineMeasure = measure;
        }
        measure.style.width = `${contentWidth}px`;
        measure.style.font = style.font;
        measure.style.letterSpacing = style.letterSpacing;
        measure.style.tabSize = style.tabSize;
        const logicalLines = editor.value.split("\n");
        this.synchronizeLineNumberRows(lineNumbers, logicalLines.length);
        const numberRows = lineNumbers.querySelectorAll(":scope > span");
        const lineHeight = Number.parseFloat(style.lineHeight) || 24;
        const previousLayout = editor._lineLayout;
        const sameWidth = previousLayout?.width === contentWidth;
        const heights = new Array(logicalLines.length);
        numberRows.forEach((row, index) => {
            const line = logicalLines[index] || " ";
            let height = sameWidth && previousLayout.lines[index] === line
                ? previousLayout.heights[index]
                : 0;
            if (!height) {
                measure.textContent = line;
                const visualHeight = Math.max(lineHeight, measure.getBoundingClientRect().height);
                height = Math.ceil(visualHeight / lineHeight) * lineHeight;
            }
            heights[index] = height;
            row.style.height = `${height}px`;
            const visualRows = Math.max(1, Math.round(height / lineHeight));
            if (row._visualRows !== visualRows) {
                row.replaceChildren();
                for (let visualIndex = 0; visualIndex < visualRows; visualIndex++) {
                    const number = document.createElement("span");
                    number.className = visualIndex === 0
                        ? "line-number-visual"
                        : "line-number-visual line-number-continuation";
                    number.textContent = String(index + 1);
                    row.appendChild(number);
                }
                row._visualRows = visualRows;
            }
        });
        editor._lineLayout = { width: contentWidth, lines: logicalLines, heights };
    },
    synchronizeLineNumberRows(container, lineCount) {
        const target = Math.max(1, lineCount);
        while (container.childElementCount < target) {
            const row = document.createElement("span");
            row.className = "line-number-row";
            container.appendChild(row);
        }
        while (container.childElementCount > target) {
            container.lastElementChild.remove();
        }
    },
    scheduleScrollSync(editor) {
        if (editor._scrollSyncFrame)
            cancelAnimationFrame(editor._scrollSyncFrame);
        editor._scrollSyncFrame = requestAnimationFrame(() => {
            editor._scrollSyncFrame = 0;
            this.syncScroll(editor);
        });
    },
    initializePropertyCompletion(editor, scriptObjects) {
        const popup = document.createElement("div");
        popup.className = "property-completion";
        popup.setAttribute("role", "listbox");
        popup.setAttribute("aria-label", "Object member suggestions");
        popup.hidden = true;
        document.body.appendChild(popup);
        editor._propertyCompletion = { popup, scriptObjects, matches: [], selected: 0, start: 0 };
        const refresh = () => this.refreshPropertyCompletion(editor);
        editor.addEventListener("input", refresh);
        editor.addEventListener("click", refresh);
        editor.addEventListener("keyup", event => {
            if (!["ArrowDown", "ArrowUp", "Enter", "Tab", "Escape"].includes(event.key))
                refresh();
        });
        editor.addEventListener("blur", () => setTimeout(() => this.hidePropertyCompletion(editor), 120));
    },
    refreshPropertyCompletion(editor) {
        const state = editor._propertyCompletion;
        if (!state || editor.selectionStart !== editor.selectionEnd)
            return this.hidePropertyCompletion(editor);
        const caret = editor.selectionStart;
        const before = editor.value.slice(0, caret);
        const match = /\b([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)?$/.exec(before);
        if (!match)
            return this.hidePropertyCompletion(editor);
        const rawProperties = state.scriptObjects[match[1]];
        if (!Array.isArray(rawProperties))
            return this.hidePropertyCompletion(editor);
        const properties = rawProperties.map(property => typeof property === "string"
            ? { name: property, description: "Object property." }
            : property);
        const prefix = match[2] ?? "";
        state.matches = properties.filter(property => property.name.startsWith(prefix));
        state.selected = Math.min(state.selected, Math.max(0, state.matches.length - 1));
        state.start = caret - prefix.length;
        if (!state.matches.length)
            return this.hidePropertyCompletion(editor);
        this.hideHoverDocumentation(editor);
        state.popup.replaceChildren(...state.matches.map((property, index) => {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "property-completion-item" + (index === state.selected ? " selected" : "");
            item.setAttribute("role", "option");
            item.setAttribute("aria-selected", index === state.selected ? "true" : "false");
            item.textContent = property.name;
            item.addEventListener("mouseenter", () => {
                const documentation = editor._hoverDocumentation;
                if (!documentation)
                    return;
                clearTimeout(documentation.timer);
                documentation.current = property;
                this.showHoverDocumentation(editor, property, item.getBoundingClientRect());
            });
            item.addEventListener("mouseleave", () => this.hideHoverDocumentation(editor));
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
        const editorBounds = editor.getBoundingClientRect();
        const editorStyle = getComputedStyle(editor);
        const paddingLeft = Number.parseFloat(editorStyle.paddingLeft) || 0;
        const paddingTop = Number.parseFloat(editorStyle.paddingTop) || 0;
        const lineHeight = Number.parseFloat(editorStyle.lineHeight) || 24;
        const characterWidth = this.editorCharacterWidth(editor);
        const anchorLeft = editorBounds.left + paddingLeft + column * characterWidth - editor.scrollLeft;
        const anchorTop = editorBounds.top + paddingTop + (line + 1) * lineHeight - editor.scrollTop;
        state.popup.hidden = false;
        this.positionPropertyCompletion(state.popup, anchorLeft, anchorTop, lineHeight);
    },
    editorCharacterWidth(editor) {
        let measure = editor._characterMeasure;
        if (!measure) {
            measure = document.createElement("span");
            measure.style.position = "fixed";
            measure.style.visibility = "hidden";
            measure.style.whiteSpace = "pre";
            measure.textContent = "0000000000";
            document.body.appendChild(measure);
            editor._characterMeasure = measure;
        }
        measure.style.font = getComputedStyle(editor).font;
        return measure.getBoundingClientRect().width / 10 || 8.43;
    },
    positionPropertyCompletion(popup, anchorLeft, anchorTop, lineHeight) {
        const margin = 8;
        const bounds = popup.getBoundingClientRect();
        const left = Math.min(
            window.innerWidth - bounds.width - margin,
            Math.max(margin, anchorLeft));
        const below = anchorTop + 4;
        const top = below + bounds.height <= window.innerHeight - margin
            ? below
            : Math.max(margin, anchorTop - lineHeight - bounds.height - 4);
        popup.style.left = `${left}px`;
        popup.style.top = `${top}px`;
    },
    handleCompletionKey(editor, event) {
        const state = editor._propertyCompletion;
        if (!state || state.popup.hidden)
            return false;
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
        if (!property)
            return;
        editor.setRangeText(property.name, state.start, editor.selectionStart, "end");
        editor.dispatchEvent(new Event("input", { bubbles: true }));
        this.hidePropertyCompletion(editor);
        editor.focus();
    },
    hidePropertyCompletion(editor) {
        const state = editor._propertyCompletion;
        if (!state)
            return;
        state.popup.hidden = true;
        state.matches = [];
        state.selected = 0;
        this.hideHoverDocumentation(editor);
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
            navigator.clipboard.writeText(removed + "\n").catch(() => { });
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
        editor.setSelectionRange(newBlockStart + relativeStart, newBlockStart + relativeEnd);
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
        if (!editor)
            return;
        const lineNumbers = document.getElementById("line-number-content");
        if (lineNumbers)
            lineNumbers.style.transform = `translate3d(0, ${-editor.scrollTop}px, 0)`;
        const highlights = document.getElementById("highlight-code");
        if (highlights) {
            highlights.style.transform =
                `translate3d(${-editor.scrollLeft}px, ${-editor.scrollTop}px, 0)`;
        }
    },
    selectRange(editorId, start, length) {
        const editor = document.getElementById(editorId);
        if (!editor)
            return;
        editor.focus();
        editor.setSelectionRange(start, start + length);
        const lineHeight = parseFloat(getComputedStyle(editor).lineHeight) || 24;
        const line = editor.value.slice(0, start).split("\n").length;
        editor.scrollTop = Math.max(0, (line - 3) * lineHeight);
        this.syncScroll(editor);
    },
    changeIndentation(editorId, outdent) {
        const editor = document.getElementById(editorId);
        if (!editor)
            return;
        const start = editor.selectionStart;
        const end = editor.selectionEnd;
        if (start !== end) {
            this.editSelectedLines(
                editorId,
                outdent
                    ? line => line.replace(/^(?: {1,2}|\t)/, "")
                    : line => `  ${line}`);
            return;
        }

        if (!outdent) {
            editor.setRangeText("  ", start, end, "end");
            editor.dispatchEvent(new Event("input", { bubbles: true }));
            return;
        }

        const lineStart = editor.value.lastIndexOf("\n", Math.max(0, start - 1)) + 1;
        const indentation = editor.value.slice(lineStart).match(/^(?: {1,2}|\t)/)?.[0];
        if (!indentation)
            return;
        editor.setRangeText("", lineStart, lineStart + indentation.length, "end");
        const caret = Math.max(lineStart, start - indentation.length);
        editor.setSelectionRange(caret, caret);
        editor.dispatchEvent(new Event("input", { bubbles: true }));
    },

    commentSelection(editorId) {
        this.editSelectedLines(editorId, line => `// ${line}`);
    },

    uncommentSelection(editorId) {
        this.editSelectedLines(editorId, line => line.replace(/^(\s*)\/\/[ ]?/, "$1"));
    },

    editSelectedLines(editorId, transform) {
        const editor = document.getElementById(editorId);
        if (!editor) return;

        const selectionStart = editor.selectionStart;
        const selectionEnd = editor.selectionEnd;
        const blockStart = editor.value.lastIndexOf("\n", Math.max(0, selectionStart - 1)) + 1;
        const effectiveEnd = selectionEnd > selectionStart && editor.value[selectionEnd - 1] === "\n"
            ? selectionEnd - 1
            : selectionEnd;
        let blockEnd = editor.value.indexOf("\n", effectiveEnd);
        if (blockEnd < 0) blockEnd = editor.value.length;
        const original = editor.value.slice(blockStart, blockEnd);
        const replacement = original.split("\n").map(transform).join("\n");
        editor.setRangeText(replacement, blockStart, blockEnd, "select");
        editor.setSelectionRange(blockStart, blockStart + replacement.length);
        editor.dispatchEvent(new Event("input", { bubbles: true }));
        editor.focus({ preventScroll: true });
    }
};
