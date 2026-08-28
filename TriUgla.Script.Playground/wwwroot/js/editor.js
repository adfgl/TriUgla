window.editorInterop = {
    initialize(editorId) {
        const editor = document.getElementById(editorId);
        if (!editor || editor.dataset.initialized) return;

        editor.dataset.initialized = "true";
        editor.addEventListener("keydown", event => {
            if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
                event.preventDefault();
                return;
            }

            if (event.key !== "Tab") return;

            event.preventDefault();
            this.insertTab(editorId);
        });
    },

    syncScroll(editor) {
        const gutter = document.getElementById("line-numbers");
        if (gutter) gutter.scrollTop = editor.scrollTop;
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
