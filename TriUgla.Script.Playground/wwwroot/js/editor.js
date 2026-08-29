window.editorInterop = {
    initialize(editorId, scriptObjects = {}) {
        const editor = document.getElementById(editorId);
        if (!editor || editor.dataset.initialized)
            return;
        editor.dataset.initialized = "true";
        this.initializePropertyCompletion(editor, scriptObjects);
        this.initializeScrollSync(editor);
        this.initializeHoverDocumentation(editor);
        editor.addEventListener("keydown", event => {
            if (event.isComposing)
                return;
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
            this.insertTab(editorId);
        });
    },
    initializeHoverDocumentation(editor) {
        const tooltip = document.getElementById("lexeme-tooltip");
        if (!tooltip)
            return;
        editor._hoverDocumentation = { tooltip, items: [], current: null, timer: 0 };
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
    },
    updateHoverDocumentation(editor, event) {
        const state = editor._hoverDocumentation;
        if (!state)
            return;
        const offset = this.sourceOffsetAt(editor, event.clientX, event.clientY);
        const item = offset < 0
            ? null
            : state.items.find(candidate => offset >= candidate.start && offset < candidate.start + candidate.length);
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
            this.showHoverDocumentation(editor, item, event.clientX, event.clientY);
        }, 280);
    },
    sourceOffsetAt(editor, clientX, clientY) {
        const bounds = editor.getBoundingClientRect();
        const style = getComputedStyle(editor);
        const lineHeight = Number.parseFloat(style.lineHeight);
        const paddingLeft = Number.parseFloat(style.paddingLeft);
        const paddingTop = Number.parseFloat(style.paddingTop);
        if (!Number.isFinite(lineHeight) || lineHeight <= 0)
            return -1;
        if (!this._fontMeasureCanvas)
            this._fontMeasureCanvas = document.createElement("canvas");
        const canvas = this._fontMeasureCanvas;
        const context = canvas.getContext("2d");
        context.font = style.font;
        const characterWidth = context.measureText("M").width;
        const x = clientX - bounds.left - paddingLeft + editor.scrollLeft;
        const y = clientY - bounds.top - paddingTop + editor.scrollTop;
        if (x < 0 || y < 0 || characterWidth <= 0)
            return -1;
        const lines = editor.value.split("\n");
        const lineIndex = Math.floor(y / lineHeight);
        if (lineIndex < 0 || lineIndex >= lines.length)
            return -1;
        const column = Math.floor(x / characterWidth);
        if (column < 0 || column >= lines[lineIndex].length)
            return -1;
        let offset = column;
        for (let index = 0; index < lineIndex; index++)
            offset += lines[index].length + 1;
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
        const numberRows = lineNumbers.querySelectorAll(":scope > span");
        const logicalLines = editor.value.split("\n");
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
        });
        editor._lineLayout = { width: contentWidth, lines: logicalLines, heights };
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
        const surface = editor.closest(".code-surface");
        if (!surface)
            return;
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
        const properties = state.scriptObjects[match[1]];
        if (!Array.isArray(properties))
            return this.hidePropertyCompletion(editor);
        const prefix = match[2] ?? "";
        state.matches = properties.filter(name => name.startsWith(prefix));
        state.selected = Math.min(state.selected, Math.max(0, state.matches.length - 1));
        state.start = caret - prefix.length;
        if (!state.matches.length)
            return this.hidePropertyCompletion(editor);
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
        editor.setRangeText(property, state.start, editor.selectionStart, "end");
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
    },
    initializeSplitters(workspaceSelector) {
        const workspace = document.querySelector(workspaceSelector);
        if (!workspace || workspace.dataset.splittersInitialized)
            return;
        workspace.dataset.splittersInitialized = "true";
        const vertical = document.getElementById("editor-output-splitter");
        const horizontal = document.getElementById("main-diagnostics-splitter");
        const rightHorizontal = document.getElementById("viewer-output-splitter");
        this.initializeSplitter(workspace, vertical, "vertical");
        this.initializeSplitter(workspace, horizontal, "diagnostics");
        this.initializeSplitter(workspace, rightHorizontal, "output");
    },
    initializeSplitter(workspace, splitter, orientation) {
        if (!splitter)
            return;
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
            if (!valid)
                return;
            event.preventDefault();
            const editor = workspace.querySelector(".editor-panel");
            const diagnostics = workspace.querySelector(".diagnostics-panel");
            const step = event.shiftKey ? 48 : 16;
            if (orientation === "vertical") {
                const direction = event.key === "ArrowLeft" ? -1 : 1;
                const x = workspace.getBoundingClientRect().left + editor.getBoundingClientRect().width + direction * step;
                this.resizePanes(workspace, orientation, x, 0);
            }
            else if (orientation === "diagnostics") {
                const direction = event.key === "ArrowUp" ? -1 : 1;
                const y = workspace.getBoundingClientRect().bottom - diagnostics.getBoundingClientRect().height + direction * step;
                this.resizePanes(workspace, orientation, 0, y);
            }
            else {
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
            const editorWidth = Math.min(bounds.width - minimum - 10, Math.max(minimum, clientX - bounds.left));
            workspace.style.setProperty("--editor-width", editorWidth + "px");
            const percentage = Math.round(editorWidth / bounds.width * 100);
            document.getElementById("editor-output-splitter")?.setAttribute("aria-valuenow", percentage);
        }
        else if (orientation === "diagnostics") {
            const header = workspace.querySelector(".app-header");
            const availableHeight = bounds.bottom - header.getBoundingClientRect().bottom - 10;
            const minimum = Math.min(96, Math.max(64, availableHeight * .2));
            const maximum = Math.max(minimum, availableHeight - 140);
            const diagnosticsHeight = Math.min(maximum, Math.max(minimum, bounds.bottom - clientY));
            workspace.style.setProperty("--diagnostics-height", diagnosticsHeight + "px");
            const percentage = Math.round(diagnosticsHeight / availableHeight * 100);
            document.getElementById("main-diagnostics-splitter")?.setAttribute("aria-valuenow", percentage);
        }
        else {
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
        if (editor)
            this.syncScroll(editor);
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
    insertTab(editorId) {
        const editor = document.getElementById(editorId);
        if (!editor)
            return;
        const start = editor.selectionStart;
        const end = editor.selectionEnd;
        editor.value = editor.value.slice(0, start) + "  " + editor.value.slice(end);
        editor.setSelectionRange(start + 2, start + 2);
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
